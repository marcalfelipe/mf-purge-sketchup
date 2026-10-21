import ctypes
import json
import os
import subprocess
import sys
import time
from pathlib import Path

if sys.stdout is not None:
    sys.stdout.reconfigure(encoding="utf-8")
if sys.stderr is not None:
    sys.stderr.reconfigure(encoding="utf-8")

# Limites da busca isolada usada quando a gravação falha mesmo com as proteções.
MAX_FALLBACK_ATTEMPTS = 40
MIN_ATTEMPT_TIMEOUT = 300


def emit_progress(stage, percent, message, **details):
    payload = {
        "type": "progress",
        "stage": stage,
        "message": message,
        **details,
    }
    if percent is not None:
        payload["percent"] = max(0, min(100, int(percent)))
    try:
        print(json.dumps(payload, ensure_ascii=False), flush=True)
    except (AttributeError, OSError):
        pass


class Ref(ctypes.Structure):
    _fields_ = [("ptr", ctypes.c_void_p)]


class ApiCrash(RuntimeError):
    """A API sofreu uma violação de acesso; o processo não deve mais chamá-la."""


def fail(result, operation):
    if result != 0:
        raise RuntimeError(f"A API do SketchUp falhou ao {operation} (código {result}).")


def same_file(a, b):
    if os.path.normcase(os.path.realpath(a)) == os.path.normcase(os.path.realpath(b)):
        return True
    try:
        return os.path.exists(a) and os.path.exists(b) and os.path.samefile(a, b)
    except OSError:
        return False


def ensure_distinct_paths(input_path, *other_paths):
    for path in other_paths:
        if same_file(input_path, path):
            raise RuntimeError(
                "O arquivo de saída coincide com o arquivo de entrada. "
                "A operação foi cancelada para não sobrescrever o original."
            )


class SketchUpApi:
    def __init__(self):
        executable_dir = Path(sys.executable).resolve().parent
        source_dir = Path(__file__).resolve().parent
        local_dirs = list(dict.fromkeys((executable_dir, source_dir)))
        candidates = [
            directory / "SketchUpAPI.dll"
            for directory in local_dirs
            if (directory / "SketchUpAPI.dll").is_file()
            and (directory / "SketchUpCommonPreferences.dll").is_file()
        ]

        program_files = os.environ.get("ProgramFiles", r"C:\Program Files")
        installed = [
            path
            for path in sorted(
                Path(program_files).glob("SketchUp/SketchUp */SketchUp/SketchUpAPI.dll"),
                reverse=True,
            )
            if (path.parent / "SketchUpCommonPreferences.dll").is_file()
        ]
        candidates.extend(path for path in installed if path not in candidates)
        if not candidates:
            raise RuntimeError(
                "O runtime oficial da API do SketchUp não foi encontrado no pacote nem em uma instalação local."
            )

        self.api_path = candidates[0]
        os.add_dll_directory(str(self.api_path.parent))
        self.lib = ctypes.CDLL(str(self.api_path))
        self._configure()

    def _function(self, name, args, result=ctypes.c_int):
        function = getattr(self.lib, name)
        function.argtypes = args
        function.restype = result
        return function

    def _optional(self, name, args, result=ctypes.c_int):
        try:
            return self._function(name, args, result)
        except AttributeError:
            return None

    def _configure(self):
        p_ref = ctypes.POINTER(Ref)
        p_size = ctypes.POINTER(ctypes.c_size_t)

        self.initialize = self._function("SUInitialize", [], None)
        self.terminate = self._function("SUTerminate", [], None)
        self.load = self._function(
            "SUModelCreateFromFileWithStatus",
            [p_ref, ctypes.c_char_p, ctypes.POINTER(ctypes.c_int)],
        )
        self.save = self._function("SUModelSaveToFile", [Ref, ctypes.c_char_p])
        self.release_model = self._function("SUModelRelease", [p_ref])
        self.model_entities = self._function("SUModelGetEntities", [Ref, p_ref])

        self.string_create = self._function("SUStringCreate", [p_ref])
        self.string_release = self._function("SUStringRelease", [p_ref])
        self.string_length = self._function("SUStringGetUTF8Length", [Ref, p_size])
        self.string_utf8 = self._function("SUStringGetUTF8", [Ref, ctypes.c_size_t, ctypes.c_char_p, p_size])

        self.count_components = self._function("SUModelGetNumComponentDefinitions", [Ref, p_size])
        self.get_components = self._function("SUModelGetComponentDefinitions", [Ref, ctypes.c_size_t, p_ref, p_size])
        self.count_groups = self._function("SUModelGetNumGroupDefinitions", [Ref, p_size])
        self.get_groups = self._function("SUModelGetGroupDefinitions", [Ref, ctypes.c_size_t, p_ref, p_size])
        self.count_images = self._function("SUModelGetNumImageDefinitions", [Ref, p_size])
        self.get_images = self._function("SUModelGetImageDefinitions", [Ref, ctypes.c_size_t, p_ref, p_size])
        self.definition_name = self._function("SUComponentDefinitionGetName", [Ref, p_ref])
        self.definition_used = self._function("SUComponentDefinitionGetNumUsedInstances", [Ref, p_size])
        self.definition_entities = self._function("SUComponentDefinitionGetEntities", [Ref, p_ref])
        self.count_definition_instances = self._function("SUComponentDefinitionGetNumInstances", [Ref, p_size])
        self.get_definition_instances = self._function(
            "SUComponentDefinitionGetInstances", [Ref, ctypes.c_size_t, p_ref, p_size]
        )
        self.instance_to_entity = self._function("SUComponentInstanceToEntity", [Ref], Ref)
        self.remove_definitions = self._function("SUModelRemoveComponentDefinitions", [Ref, ctypes.c_size_t, p_ref])
        self.definition_is_live = self._function(
            "SUComponentDefinitionIsLiveComponent",
            [Ref, ctypes.POINTER(ctypes.c_bool)],
        )

        # Referências que cenas e o modelo guardam para entidades dentro de
        # definições. Remover a definição dona deixa essas referências
        # pendentes e o SUModelSaveToFile falha (código 7) ou viola memória.
        self.entity_parent = self._optional("SUEntityGetParentEntities", [Ref, p_ref])
        self.section_plane_to_entity = self._optional("SUSectionPlaneToEntity", [Ref], Ref)
        self.count_scenes = self._optional("SUModelGetNumScenes", [Ref, p_size])
        self.get_scenes = self._optional("SUModelGetScenes", [Ref, ctypes.c_size_t, p_ref, p_size])
        self.scene_name = self._optional("SUSceneGetName", [Ref, p_ref])
        self.count_scene_planes = self._optional("SUSceneGetNumActiveSectionPlanes", [Ref, p_size])
        self.get_scene_planes = self._optional("SUSceneGetActiveSectionPlanes", [Ref, ctypes.c_size_t, p_ref, p_size])
        self.count_scene_hidden = self._optional("SUSceneGetNumHiddenEntities", [Ref, p_size])
        self.get_scene_hidden = self._optional("SUSceneGetHiddenEntities", [Ref, ctypes.c_size_t, p_ref, p_size])
        self.count_model_planes = self._optional("SUModelGetNumActiveSectionPlanes", [Ref, p_size])
        self.get_model_planes = self._optional("SUModelGetActiveSectionPlanes", [Ref, ctypes.c_size_t, p_ref, p_size])

        self.count_materials = self._function("SUModelGetNumMaterials", [Ref, p_size])
        self.get_materials = self._function("SUModelGetMaterials", [Ref, ctypes.c_size_t, p_ref, p_size])
        self.remove_materials = self._function("SUModelRemoveMaterials", [Ref, ctypes.c_size_t, p_ref])
        self.material_type = self._function("SUMaterialGetType", [Ref, ctypes.POINTER(ctypes.c_int)])
        self.element_material = self._function("SUDrawingElementGetMaterial", [Ref, p_ref])
        self.front_material = self._function("SUFaceGetFrontMaterial", [Ref, p_ref])
        self.back_material = self._function("SUFaceGetBackMaterial", [Ref, p_ref])

        # SUEntitiesGetNumEdges/GetEdges recebem "standalone_only" antes do
        # ponteiro de saída; sem ele a API escreve num endereço arbitrário.
        count_edges = self._function("SUEntitiesGetNumEdges", [Ref, ctypes.c_bool, p_size])
        get_edges = self._function("SUEntitiesGetEdges", [Ref, ctypes.c_bool, ctypes.c_size_t, p_ref, p_size])
        self.entity_getters = [(
            lambda entities, count: count_edges(entities, False, count),
            lambda entities, size, refs, count: get_edges(entities, False, size, refs, count),
            self._function("SUEdgeToDrawingElement", [Ref], Ref),
        )]
        for suffix, element in (
            ("Groups", "Group"), ("Images", "Image"), ("Instances", "ComponentInstance"),
            ("GuideLines", "GuideLine"), ("GuidePoints", "GuidePoint"), ("SectionPlanes", "SectionPlane"),
            ("Texts", "Text"), ("Dimensions", "Dimension"), ("Polyline3ds", "Polyline3d"),
        ):
            self.entity_getters.append((
                self._function(f"SUEntitiesGetNum{suffix}", [Ref, p_size]),
                self._function(f"SUEntitiesGet{suffix}", [Ref, ctypes.c_size_t, p_ref, p_size]),
                self._function(f"SU{element}ToDrawingElement", [Ref], Ref),
            ))
        self.count_faces = self._function("SUEntitiesGetNumFaces", [Ref, p_size])
        self.get_faces = self._function("SUEntitiesGetFaces", [Ref, ctypes.c_size_t, p_ref, p_size])
        self.face_to_element = self._function("SUFaceToDrawingElement", [Ref], Ref)

    @staticmethod
    def get_refs(parent, count_fn, get_fn):
        count = ctypes.c_size_t()
        fail(count_fn(parent, ctypes.byref(count)), "contar itens do modelo")
        if count.value == 0:
            return []
        refs = (Ref * count.value)()
        received = ctypes.c_size_t()
        fail(get_fn(parent, count.value, refs, ctypes.byref(received)), "ler itens do modelo")
        return [Ref(refs[i].ptr) for i in range(received.value)]

    def string(self, getter, ref):
        text = Ref()
        fail(self.string_create(ctypes.byref(text)), "criar texto")
        try:
            if getter(ref, ctypes.byref(text)) != 0:
                return "?"
            length = ctypes.c_size_t()
            self.string_length(text, ctypes.byref(length))
            buffer = ctypes.create_string_buffer(length.value + 1)
            copied = ctypes.c_size_t()
            self.string_utf8(text, length.value + 1, buffer, ctypes.byref(copied))
            return buffer.value.decode("utf-8", "replace")
        finally:
            self.string_release(ctypes.byref(text))

    def definition_label(self, definition):
        return self.string(self.definition_name, definition)

    def definitions(self, model):
        refs = []
        refs.extend(self.get_refs(model, self.count_components, self.get_components))
        refs.extend(self.get_refs(model, self.count_groups, self.get_groups))
        refs.extend(self.get_refs(model, self.count_images, self.get_images))
        unique = {}
        for ref in refs:
            unique[ref.ptr] = ref
        return list(unique.values())

    def definitions_of_kind(self, model, kind):
        if kind == "normal":
            return self.get_refs(model, self.count_components, self.get_components)
        if kind == "groups":
            return self.get_refs(model, self.count_groups, self.get_groups)
        if kind == "images":
            return self.get_refs(model, self.count_images, self.get_images)
        return self.definitions(model)

    def materials(self, model):
        return self.get_refs(model, self.count_materials, self.get_materials)

    def textured_material_count(self, materials):
        total = 0
        for material in materials:
            kind = ctypes.c_int()
            if self.material_type(material, ctypes.byref(kind)) == 0 and kind.value in (1, 2):
                total += 1
        return total

    def used_instance_count(self, definition):
        count = ctypes.c_size_t()
        fail(self.definition_used(definition, ctypes.byref(count)), "contar instâncias de componentes")
        return count.value

    def is_live_component(self, definition):
        is_live = ctypes.c_bool()
        fail(self.definition_is_live(definition, ctypes.byref(is_live)), "verificar componente Live")
        return is_live.value

    def scene_referenced_definitions(self, model, definitions):
        """Definições que contêm entidades referenciadas por cenas ou pelo modelo.

        Retorna {ptr da definição: [motivos]}.
        """
        required = (
            self.entity_parent, self.section_plane_to_entity, self.count_scenes, self.get_scenes,
            self.scene_name, self.count_scene_planes, self.get_scene_planes,
            self.count_scene_hidden, self.get_scene_hidden,
        )
        if not all(required):
            emit_progress(
                "componentes", 39,
                "Aviso: esta versão da API não permite verificar referências de cenas; "
                "a gravação será validada e haverá fallback se necessário.",
            )
            return {}

        owner_by_entities = {}
        for definition in definitions:
            entities = Ref()
            if self.definition_entities(definition, ctypes.byref(entities)) == 0 and entities.ptr:
                owner_by_entities[entities.ptr] = definition.ptr
        definition_by_instance = None
        protected = {}

        def protect(owner, reason):
            if owner:
                reasons = protected.setdefault(owner, [])
                if reason not in reasons:
                    reasons.append(reason)

        def owner_of(entity):
            parent = Ref()
            if self.entity_parent(entity, ctypes.byref(parent)) != 0:
                return None
            return owner_by_entities.get(parent.ptr)

        for scene in self.get_refs(model, self.count_scenes, self.get_scenes):
            name = self.string(self.scene_name, scene)
            for plane in self.get_refs(scene, self.count_scene_planes, self.get_scene_planes):
                protect(owner_of(self.section_plane_to_entity(plane)), f"plano de seção ativo da cena “{name}”")
            hidden = self.get_refs(scene, self.count_scene_hidden, self.get_scene_hidden)
            if hidden and definition_by_instance is None:
                definition_by_instance = {}
                for definition in definitions:
                    for instance in self.get_refs(
                        definition, self.count_definition_instances, self.get_definition_instances
                    ):
                        definition_by_instance[self.instance_to_entity(instance).ptr] = definition.ptr
            for entity in hidden:
                protect(owner_of(entity), f"entidade oculta pela cena “{name}”")
                protect(definition_by_instance.get(entity.ptr), f"instância oculta pela cena “{name}”")

        if self.count_model_planes and self.get_model_planes:
            for plane in self.get_refs(model, self.count_model_planes, self.get_model_planes):
                protect(owner_of(self.section_plane_to_entity(plane)), "plano de seção ativo do modelo")
        return protected

    def select_unused_definitions(self, model, kind=None, preserve_live=False, protect_scene_refs=True):
        """Retorna (definições, índices removíveis, [(nome, motivos)] preservadas)."""
        definitions = self.definitions_of_kind(model, kind)
        protected = {}
        if protect_scene_refs:
            emit_progress("componentes", 39, "Verificando referências de cenas e planos de seção…")
            protected = self.scene_referenced_definitions(model, self.definitions(model))
        selected = []
        preserved = []
        live_skipped = 0
        total = len(definitions)
        emit_progress("componentes", 40, f"Analisando {total} definições de componentes e grupos…")
        interval = max(1, total // 50)
        for index, definition in enumerate(definitions, 1):
            if preserve_live and self.is_live_component(definition):
                live_skipped += 1
            elif self.used_instance_count(definition) == 0:
                if definition.ptr in protected:
                    preserved.append((self.definition_label(definition), protected[definition.ptr]))
                else:
                    selected.append(index - 1)
            if index == total or index % interval == 0:
                percent = 40 + (12 * index // max(1, total))
                emit_progress("componentes", percent, f"Componentes analisados: {index}/{total}")
        if preserve_live:
            emit_progress("componentes", 52, f"Definições Live preservadas: {live_skipped}")
        for name, reasons in preserved:
            emit_progress(
                "componentes", 52,
                f"Fallback: definição não usada “{name}” preservada porque é referenciada "
                f"({'; '.join(reasons[:3])}{'…' if len(reasons) > 3 else ''}).",
            )
        return definitions, selected, preserved

    def remove_definition_indices(self, model, definitions, indices):
        if indices:
            array = (Ref * len(indices))(*[definitions[i] for i in indices])
            fail(self.remove_definitions(model, len(indices), array), "remover componentes não utilizados")
        emit_progress("componentes", 54, f"Componentes/grupos não utilizados removidos: {len(indices)}")

    def purge_definitions(
        self,
        model,
        kind=None,
        preserve_live=False,
        slice_start=None,
        slice_count=None,
        protect_scene_refs=True,
    ):
        definitions, selected, preserved = self.select_unused_definitions(
            model, kind, preserve_live, protect_scene_refs
        )
        if slice_start is not None:
            total = len(selected)
            selected = selected[slice_start:slice_start + slice_count]
            emit_progress(
                "componentes",
                53,
                f"Faixa diagnóstica: {slice_start}–{slice_start + len(selected) - 1} de {total} candidatos",
            )
        self.remove_definition_indices(model, definitions, selected)
        return definitions, selected, preserved

    def add_element_material(self, element, used):
        material = Ref()
        if self.element_material(element, ctypes.byref(material)) == 0 and material.ptr:
            used.add(material.ptr)

    def collect_used_materials(self, entities, used):
        for count_fn, get_fn, to_element in self.entity_getters:
            for element in self.get_refs(entities, count_fn, get_fn):
                self.add_element_material(to_element(element), used)

        for face in self.get_refs(entities, self.count_faces, self.get_faces):
            self.add_element_material(self.face_to_element(face), used)
            for getter in (self.front_material, self.back_material):
                material = Ref()
                if getter(face, ctypes.byref(material)) == 0 and material.ptr:
                    used.add(material.ptr)

    def purge_materials_by_scan(self, model, include_unused_definitions=False):
        used = set()
        root = Ref()
        emit_progress("materiais", 56, "Analisando materiais usados na geometria principal…")
        fail(self.model_entities(model, ctypes.byref(root)), "ler as entidades do modelo")
        self.collect_used_materials(root, used)

        definitions = self.definitions(model)
        total = len(definitions)
        interval = max(1, total // 50)
        for index, definition in enumerate(definitions, 1):
            if include_unused_definitions or self.used_instance_count(definition) > 0:
                entities = Ref()
                fail(self.definition_entities(definition, ctypes.byref(entities)), "ler uma definição de componente")
                self.collect_used_materials(entities, used)
            if index == total or index % interval == 0:
                percent = 60 + (18 * index // max(1, total))
                emit_progress("materiais", percent, f"Definições verificadas para materiais: {index}/{total}")

        unused = [material for material in self.materials(model) if material.ptr not in used]
        if unused:
            array = (Ref * len(unused))(*unused)
            fail(self.remove_materials(model, len(unused), array), "remover materiais não utilizados")
        emit_progress("materiais", 80, f"Materiais não utilizados removidos: {len(unused)}")

    def purge_materials(self, model):
        # Definições removidas já levaram suas referências; as que restam
        # (inclusive as preservadas por segurança) continuam usando seus
        # materiais, então todas entram na varredura.
        self.purge_materials_by_scan(model, include_unused_definitions=True)

    def inventory(self, model):
        materials = self.materials(model)
        return {
            "definicoes": len(self.definitions(model)),
            "materiais": len(materials),
            "texturas": self.textured_material_count(materials),
        }


def call_api(function, *args):
    """Chama a API convertendo violações de acesso em ApiCrash."""
    try:
        return function(*args)
    except OSError as error:
        raise ApiCrash(f"Violação de acesso dentro da API do SketchUp: {error}") from error


def load_model(api, path):
    model = Ref()
    status = ctypes.c_int()
    fail(call_api(api.load, ctypes.byref(model), path.encode("utf-8"), ctypes.byref(status)), "ler o arquivo .skp")
    if status.value == 1:
        call_api(api.release_model, ctypes.byref(model))
        raise RuntimeError(
            "O modelo foi criado por uma versão mais recente que a API instalada. "
            "A gravação foi cancelada para evitar perda de dados."
        )
    return model


def verify_reopen(api, path):
    if not os.path.isfile(path) or os.path.getsize(path) == 0:
        raise RuntimeError("A API não produziu um arquivo de saída válido.")
    reopened = Ref()
    status = ctypes.c_int()
    result = call_api(api.load, ctypes.byref(reopened), path.encode("utf-8"), ctypes.byref(status))
    if reopened.ptr:
        call_api(api.release_model, ctypes.byref(reopened))
    if result != 0:
        raise RuntimeError(f"A saída gravada não pôde ser reaberta pela API do SketchUp (código {result}).")


def write_result(path, data):
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_suffix(path.suffix + ".tmp")
    temporary.write_text(json.dumps(data, ensure_ascii=False), encoding="utf-8")
    os.replace(temporary, path)


def remove_quietly(path):
    try:
        if os.path.exists(path):
            os.remove(path)
    except OSError:
        pass


def run_attempt(input_path, output_path, result_path, plan_path):
    """Processo filho do fallback: aplica um plano de remoção e valida a gravação."""
    plan = json.loads(Path(plan_path).read_text(encoding="utf-8"))
    ensure_distinct_paths(input_path, output_path, result_path)
    api = SketchUpApi()
    api.initialize()
    model = Ref()
    try:
        model = load_model(api, input_path)
        definitions = api.definitions(model)
        if len(definitions) != plan["total_definicoes"]:
            raise RuntimeError("O inventário de definições mudou entre as tentativas.")
        for index, expected in zip(plan["remover"], plan["nomes"]):
            if api.definition_label(definitions[index]) != expected:
                raise RuntimeError("A ordem das definições mudou entre as tentativas.")
        before = api.inventory(model)
        api.remove_definition_indices(model, definitions, plan["remover"])
        if plan["materiais"]:
            api.purge_materials(model)
        after = api.inventory(model)
        fail(call_api(api.save, model, output_path.encode("utf-8")), "gravar o modelo limpo")
        call_api(api.release_model, ctypes.byref(model))
        model = Ref()
        verify_reopen(api, output_path)
        write_result(result_path, {"status": "ok", "antes": before, "depois": after})
        return 0
    except Exception as error:
        remove_quietly(output_path)
        write_result(result_path, {"status": "erro", "erro": str(error)})
        return 1
    finally:
        if model.ptr:
            api.release_model(ctypes.byref(model))
        api.terminate()


class FallbackSearch:
    """Bisseção em processos isolados para achar o maior purge serializável."""

    def __init__(self, input_path, output_path, definitions_total, candidates, names, timeout):
        self.input_path = input_path
        self.output_path = output_path
        self.total = definitions_total
        self.candidates = candidates
        self.names = names
        self.timeout = timeout
        self.attempts = 0
        self.successes = {}
        self.materials = True
        self.files = []

    def command(self, *args):
        if getattr(sys, "frozen", False):
            return [sys.executable, *args]
        return [sys.executable, os.path.abspath(__file__), *args]

    def test(self, removal):
        if self.attempts >= MAX_FALLBACK_ATTEMPTS:
            raise TimeoutError
        self.attempts += 1
        removal = sorted(removal)
        number = self.attempts
        base = f"{self.output_path}.tentativa{number}"
        output, result, plan = base + ".partial.skp", base + ".json", base + ".plano.json"
        self.files.extend([output, result, plan])
        write_result(plan, {
            "total_definicoes": self.total,
            "remover": removal,
            "nomes": [self.names[i] for i in removal],
            "materiais": self.materials,
        })
        emit_progress(
            "fallback", 85 + min(10, number * 10 // MAX_FALLBACK_ATTEMPTS),
            f"Tentativa isolada {number}: removendo {len(removal)} de {len(self.candidates)} definições candidatas…",
        )
        try:
            subprocess.run(
                self.command(self.input_path, output, result, "--attempt", plan),
                stdout=subprocess.DEVNULL,
                stderr=subprocess.DEVNULL,
                timeout=self.timeout,
                creationflags=getattr(subprocess, "CREATE_NO_WINDOW", 0),
            )
            data = json.loads(Path(result).read_text(encoding="utf-8"))
        except (OSError, ValueError, subprocess.TimeoutExpired) as error:
            data = {"status": "erro", "erro": f"tentativa interrompida: {error}"}
        ok = data.get("status") == "ok"
        emit_progress(
            "fallback", None,
            f"Tentativa isolada {number}: {'gravou e reabriu' if ok else 'falhou — ' + data.get('erro', '?')}",
        )
        if ok:
            self.successes[tuple(removal)] = (output, data)
        else:
            remove_quietly(output)
        return ok

    def isolate(self, base, candidates):
        # Pré-condição: base grava; base + candidates não grava.
        if len(candidates) == 1:
            return base, list(candidates)
        half = len(candidates) // 2
        first, second = candidates[:half], candidates[half:]
        if self.test(base + first):
            return self.isolate(base + first, second)
        base, bad = self.isolate(base, first)
        if self.test(base + second):
            return base + second, bad
        base, bad_second = self.isolate(base, second)
        return base, bad + bad_second

    def run(self):
        if not self.test([]):
            self.materials = False
            emit_progress("fallback", None, "Fallback: a limpeza de materiais também impede a gravação; ela será omitida.")
            if not self.test([]):
                raise RuntimeError("Nem a gravação sem remover definições foi aceita pela API.")
        try:
            removable, problematic = self.isolate([], list(self.candidates))
        except TimeoutError:
            emit_progress("fallback", None, "Fallback: limite de tentativas atingido; usando o maior purge já validado.")
            removable = list(max(self.successes, key=len))
            problematic = [i for i in self.candidates if i not in removable]
        key = tuple(sorted(removable))
        if key not in self.successes:
            key = max(self.successes, key=len)
            problematic = [i for i in self.candidates if i not in key]
        output, data = self.successes[key]
        return output, data, problematic

    def cleanup(self, keep=None):
        for path in self.files:
            if path != keep:
                remove_quietly(path)


def main():
    input_path, output_path, result_path = map(os.path.abspath, sys.argv[1:4])
    mode = sys.argv[4] if len(sys.argv) > 4 else "--purge"
    if mode == "--attempt":
        return run_attempt(input_path, output_path, result_path, sys.argv[5])

    partial_path = output_path + ".partial.skp"
    api = None
    model = Ref()
    initialized = False
    api_crashed = False
    search = None
    components_removed = 0
    materials_removed = 0
    textures_removed = 0
    preserved_names = []
    fallback_note = None
    try:
        emit_progress("preparacao", 1, "Preparando o processamento…")
        if not os.path.isfile(input_path):
            raise RuntimeError("O modelo selecionado não foi encontrado.")
        ensure_distinct_paths(input_path, output_path, partial_path, result_path)
        if os.path.exists(partial_path):
            os.remove(partial_path)

        api = SketchUpApi()
        emit_progress("api", 3, f"API do SketchUp localizada: {api.api_path}")
        api.initialize()
        initialized = True
        emit_progress("carregamento", 5, "Carregando o modelo sem abrir o SketchUp…")
        started = time.monotonic()
        model = load_model(api, input_path)
        load_seconds = time.monotonic() - started
        emit_progress("carregamento", 35, "Modelo carregado com sucesso.")

        definitions, selected, names = [], [], {}
        if mode == "--copy-only":
            before = after = {"definicoes": 0, "materiais": 0, "texturas": 0}
        else:
            emit_progress("inventario", 37, "Contando componentes, materiais e texturas…")
            before = api.inventory(model)

            if mode in ("--purge", "--purge-unprotected"):
                definitions, selected, preserved = api.select_unused_definitions(
                    model, protect_scene_refs=(mode == "--purge")
                )
                preserved_names = [name for name, _ in preserved]
                # Nomes lidos antes da remoção: servem para conferir a ordem
                # das definições nas tentativas isoladas do fallback.
                names = {i: api.definition_label(definitions[i]) for i in selected}
                api.remove_definition_indices(model, definitions, selected)
                api.purge_materials(model)
            elif mode == "--components-only":
                api.purge_definitions(model)
            elif mode == "--normal-components-only":
                api.purge_definitions(model, "normal")
            elif mode == "--normal-components-preserve-live":
                api.purge_definitions(model, "normal", preserve_live=True)
            elif mode == "--normal-slice":
                api.purge_definitions(
                    model,
                    "normal",
                    slice_start=int(sys.argv[5]),
                    slice_count=int(sys.argv[6]),
                )
            elif mode == "--groups-only":
                api.purge_definitions(model, "groups")
            elif mode == "--images-only":
                api.purge_definitions(model, "images")
            elif mode == "--materials-only":
                api.purge_materials_by_scan(model, include_unused_definitions=True)
            else:
                raise RuntimeError(f"Modo desconhecido: {mode}")

            after = api.inventory(model)

        emit_progress("gravacao", 84, "Gravando o arquivo de saída…")
        started = time.monotonic()
        save_error = None
        try:
            fail(call_api(api.save, model, partial_path.encode("utf-8")), "gravar o modelo limpo")
            emit_progress("gravacao", 90, "Arquivo gravado; reabrindo a saída para validar…")
            call_api(api.release_model, ctypes.byref(model))
            model = Ref()
            verify_reopen(api, partial_path)
        except ApiCrash as error:
            api_crashed = True
            save_error = error
        except RuntimeError as error:
            save_error = error
        save_seconds = time.monotonic() - started

        if save_error is None:
            os.replace(partial_path, output_path)
        elif mode in ("--purge", "--purge-unprotected") and selected:
            remove_quietly(partial_path)
            emit_progress(
                "fallback", 85,
                f"Fallback: a gravação falhou ({save_error}). Procurando, em processos isolados, "
                "as definições que impedem a gravação…",
            )
            if not api_crashed:
                # Após uma violação de acesso o estado da API não é confiável;
                # nesse caso o modelo fica para o encerramento do processo.
                if model.ptr:
                    call_api(api.release_model, ctypes.byref(model))
                api.terminate()
            model = Ref()
            initialized = False
            search = FallbackSearch(
                input_path, output_path, len(definitions), selected, names,
                max(MIN_ATTEMPT_TIMEOUT, 6 * (load_seconds + save_seconds)),
            )
            attempt_output, data, problematic = search.run()
            os.replace(attempt_output, output_path)
            before, after = data["antes"], data["depois"]
            problematic_names = [names[i] for i in problematic]
            preserved_names.extend(problematic_names)
            fallback_note = (
                f"{len(problematic)} definição(ões) preservada(s) após {search.attempts} tentativas isoladas"
                + ("" if search.materials else "; limpeza de materiais omitida")
            )
            for name in problematic_names:
                emit_progress("fallback", 96, f"Fallback: definição “{name}” preservada porque impedia a gravação.")
            emit_progress("fallback", 96, f"Fallback concluído: {fallback_note}.")
        else:
            raise save_error

        if mode != "--copy-only":
            components_removed = before["definicoes"] - after["definicoes"]
            materials_removed = before["materiais"] - after["materiais"]
            textures_removed = before["texturas"] - after["texturas"]
        if preserved_names and fallback_note is None:
            fallback_note = f"{len(preserved_names)} definição(ões) não usada(s) preservada(s) por referência de cena"
        write_result(result_path, {
            "status": "ok",
            "modo": mode.removeprefix("--"),
            "componentes_removidos": components_removed,
            "materiais_removidos": materials_removed,
            "texturas_removidas": textures_removed,
            "definicoes_preservadas": preserved_names,
            "fallback": fallback_note,
        })
        emit_progress("concluido", 100, "Purge concluído com sucesso; a saída foi reaberta pela API.")
        return 0
    except Exception as error:
        api_crashed = api_crashed or isinstance(error, ApiCrash)
        emit_progress("erro", None, f"Erro: {error}")
        remove_quietly(partial_path)
        write_result(result_path, {
            "status": "erro",
            "erro": str(error),
            "modo": mode.removeprefix("--"),
            "componentes_removidos": components_removed,
            "materiais_removidos": materials_removed,
            "texturas_removidas": textures_removed,
        })
        return 1
    finally:
        if search:
            search.cleanup()
        if api and model.ptr and not api_crashed:
            api.release_model(ctypes.byref(model))
        if api and initialized and not api_crashed:
            api.terminate()


if __name__ == "__main__":
    raise SystemExit(main())
