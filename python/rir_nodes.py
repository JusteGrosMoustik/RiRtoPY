# -*- coding: utf-8 -*-
"""RiRtoPY general-node building blocks, IronPython 2.7 / CPython + pythonnet.

Run on the Revit API thread. No import or helper opens a transaction.
Native DB objects in/out; no GH trees, element tracking or automatic unit guesses.
See docs/GENERAL_NODES.md for differences from Grasshopper components.
"""
try:
    text_type = unicode
    integer_types = (int, long)
except NameError:
    text_type = str
    integer_types = (int,)


def _db():
    import clr
    clr.AddReference('RevitAPI')
    from Autodesk.Revit import DB
    return DB


def scalar_input(value):
    if isinstance(value, (list, tuple, dict)):
        raise NotImplementedError('Multiple item inputs / trees are not translated here.')
    return value


def map_element(function, value, outputs):
    if isinstance(value, dict):
        raise NotImplementedError('Tree/dictionary element inputs are not translated.')
    if not isinstance(value, (list, tuple)):
        return function(value)
    if any(isinstance(item, (list, tuple, dict)) for item in value):
        raise NotImplementedError('Only one flat element list is translated.')
    result = dict((name, []) for name in outputs)
    for element in value:
        values = function(element)
        for name in outputs: result[name].append(values[name])
    return result


def _list_of(kind, values):
    from System.Collections.Generic import List
    result = List[kind]()
    for value in values:
        result.Add(value)
    return result


def _element(element):
    if element is None or not element.IsValidObject:
        raise ValueError('A valid native Revit element is required.')
    return element


def active_document():
    import clr
    clr.AddReference('RhinoInside.Revit')
    from RhinoInside.Revit import Revit
    return document_or_active(Revit.ActiveDBDocument, allow_active=False)


def document_or_active(document=None, allow_active=True):
    if document is None and allow_active:
        return active_document()
    if document is None or not document.IsValidObject:
        raise ValueError('A valid Revit document is required.')
    return document


def element_id_value(identifier):
    # Revit 2024+ IDs are 64 bit. Never truncate to Int32.
    return identifier.Value if hasattr(identifier, 'Value') else identifier.IntegerValue


def query_element(identifier, document=None):
    """Native ElementId, integer ID or UniqueId; no references/categories/parameters."""
    DB = _db()
    document = document_or_active(document)
    if isinstance(identifier, bool):
        raise TypeError('Boolean is not an element ID.')
    if isinstance(identifier, integer_types):
        identifier = DB.ElementId(identifier)
    if not isinstance(identifier, (DB.ElementId, text_type, str)):
        raise TypeError('Expected ElementId, integer or UniqueId string.')
    return document.GetElement(identifier)


def query_elements(element_filter, document=None, limit=None):
    """RiR Query Elements subset: instance elements, full count, optional limit."""
    DB = _db()
    if element_filter is None:
        raise ValueError('Query Elements requires an explicit filter.')
    if limit is not None and (isinstance(limit, bool) or not isinstance(limit, integer_types) or limit < 0):
        raise ValueError('Limit must be a non-negative integer or None.')
    collector = DB.FilteredElementCollector(document_or_active(document))
    try:
        collector.WhereElementIsNotElementType().WherePasses(element_filter)
        count = collector.GetElementCount()
        elements = []
        if limit != 0:
            for element in collector:
                elements.append(element)
                if limit is not None and len(elements) >= limit:
                    break
        return {'Elements': elements, 'Count': count}
    finally:
        collector.Dispose()


def query_by_class(element_class, document=None, element_filter=None, types=False):
    DB = _db()
    collector = DB.FilteredElementCollector(document_or_active(document))
    try:
        collector.OfClass(element_class)
        if types: collector.WhereElementIsElementType()
        else: collector.WhereElementIsNotElementType()
        if element_filter is not None: collector.WherePasses(element_filter)
        return list(collector)
    finally:
        collector.Dispose()


def query_levels(document=None, element_filter=None):
    """Unfiltered Query Levels subset; ordered by elevation then ID."""
    levels = query_by_class(_db().Level, document, element_filter)
    return sorted(levels, key=lambda e: (e.Elevation, element_id_value(e.Id)))


def query_sheets(document=None, element_filter=None):
    """All sheets including placeholders; RiR string filters not emulated."""
    return query_by_class(_db().ViewSheet, document, element_filter)


def query_materials(document=None, element_filter=None):
    return query_by_class(_db().Material, document, element_filter)


def query_phases(document=None):
    return list(document_or_active(document).Phases)


def query_view_elements(view, element_filter=None):
    """Native view visibility query; linked models/category inputs excluded."""
    DB = _db()
    view = _element(view)
    if not DB.FilteredElementCollector.IsViewValidForElementIteration(view.Document, view.Id):
        raise ValueError('This view cannot be used for element iteration.')
    collector = DB.FilteredElementCollector(view.Document, view.Id)
    try:
        collector.WhereElementIsNotElementType()
        if element_filter is not None: collector.WherePasses(element_filter)
        return list(collector)
    finally:
        collector.Dispose()


def query_title_blocks(sheet, element_filter=None):
    DB = _db()
    sheet = _element(sheet)
    if not isinstance(sheet, DB.ViewSheet):
        raise TypeError('Expected a ViewSheet.')
    collector = DB.FilteredElementCollector(sheet.Document, sheet.Id)
    try:
        collector.OfCategory(DB.BuiltInCategory.OST_TitleBlocks).WhereElementIsNotElementType()
        if element_filter is not None: collector.WherePasses(element_filter)
        return list(collector)
    finally:
        collector.Dispose()


def category_by_id(identifier, document=None):
    DB = _db()
    if not isinstance(identifier, DB.ElementId): identifier = DB.ElementId(identifier)
    return DB.Category.GetCategory(document_or_active(document), identifier)


def element_category(element):
    return _element(element).Category


def element_type(element):
    element = _element(element)
    return element.Document.GetElement(element.GetTypeId())


def element_name(element):
    """RiR logical name, which can differ from native DB.Element.Name."""
    import clr
    clr.AddReference('RhinoInside.Revit.GH')
    from RhinoInside.Revit.GH.Types import Element
    return Element.FromElement(_element(element)).Nomen


def element_identity(element):
    """Legacy Element Identity native values; Name is DB.Element.Name here."""
    element = _element(element)
    return {'Category': element.Category, 'Type': element_type(element), 'Name': element.Name}


def element_owner_view(element):
    element = _element(element)
    return {'Element': element, 'View Specific': element.ViewSpecific,
            'View': element.Document.GetElement(element.OwnerViewId) if element.ViewSpecific else None}


def element_pinned(element):
    return _element(element).Pinned


def category_filter(categories, inverted=False):
    DB = _db()
    identifiers = []
    for category in categories:
        value = category.Id if isinstance(category, DB.Category) else category
        identifiers.append(value if isinstance(value, DB.ElementId) else DB.ElementId(value))
    if not identifiers:
        raise ValueError('Empty category filters are not translated.')
    return DB.ElementMulticategoryFilter(_list_of(DB.ElementId, identifiers), bool(inverted))


def class_filter(element_class, inverted=False):
    return _db().ElementClassFilter(element_class, bool(inverted))


def exclude_element_types_filter(inverted=False):
    # RiR Exclude Element Types: inverted=False excludes types.
    return _db().ElementIsElementTypeFilter(not bool(inverted))


def logical_and(filters):
    filters = [f for f in filters if f is not None]
    if not filters: raise ValueError('Empty logical intersection is not translated.')
    if len(filters) == 1: return filters[0]
    DB = _db()
    return DB.LogicalAndFilter(_list_of(DB.ElementFilter, filters))


def logical_or(filters):
    filters = [f for f in filters if f is not None]
    if not filters: raise ValueError('Empty logical union is not translated.')
    if len(filters) == 1: return filters[0]
    DB = _db()
    return DB.LogicalOrFilter(_list_of(DB.ElementFilter, filters))


def parameter_filter(rules, inverted=False):
    rules = list(rules)
    if not rules: raise ValueError('Empty parameter rules are not translated.')
    DB = _db()
    return DB.ElementParameterFilter(_list_of(DB.FilterRule, rules), bool(inverted))


def element_parameters(element, name=None, read_only=None):
    """Native parameters; exact name filter, not RiR symbol-name patterns/grouping."""
    parameters = list(_element(element).Parameters)
    return [p for p in parameters if (name is None or p.Definition.Name == name)
            and (read_only is None or p.IsReadOnly == read_only)]


def resolve_parameter(element, key):
    """BuiltInParameter, shared GUID, native Definition or unambiguous exact name."""
    element = _element(element)
    if isinstance(key, (text_type, str)):
        parameters = list(element.GetParameters(key))
        if len(parameters) > 1:
            raise ValueError('Ambiguous parameter name: ' + key)
        return parameters[0] if parameters else None
    return element.get_Parameter(key)


def parameter_value(parameter, unit=None):
    """Raw native storage value, optional explicit unit for doubles.

    No automatic GH enum, YesNo, ElementId or Rhino-unit coercion is claimed.
    """
    DB = _db()
    if parameter is None: raise ValueError('Parameter not found.')
    if not parameter.HasValue: return None
    storage = parameter.StorageType
    if storage == DB.StorageType.Double:
        value = parameter.AsDouble()
        return DB.UnitUtils.ConvertFromInternalUnits(value, unit) if unit is not None else value
    if unit is not None: raise TypeError('Unit conversion requires a Double parameter.')
    if storage == DB.StorageType.Integer: return parameter.AsInteger()
    if storage == DB.StorageType.String: return parameter.AsString()
    if storage == DB.StorageType.ElementId: return parameter.AsElementId()
    return None


def parameter_text(parameter):
    if parameter is None: raise ValueError('Parameter not found.')
    return parameter.AsValueString()


def element_parameter(element, key, unit=None):
    return parameter_value(resolve_parameter(element, key), unit)


def element_materials(element, painted=False):
    element = _element(element)
    return [element.Document.GetElement(i) for i in element.GetMaterialIds(bool(painted))]


def material_quantities(element, material, painted=False):
    """Native internal units: ft2 area, ft3 volume; paint has no volume output."""
    element, material = _element(element), _element(material)
    if not element.Document.Equals(material.Document):
        raise ValueError('Element and material must belong to the same document.')
    return {'Area': element.GetMaterialArea(material.Id, bool(painted)),
            'Volume': None if painted else element.GetMaterialVolume(material.Id)}


def _writable(element):
    element = _element(element)
    if element.Document.IsReadOnly or not element.Document.IsModifiable:
        raise RuntimeError('An open caller-owned Revit transaction is required.')
    return element


def set_parameter(element, key, value, unit=None):
    """Native setter, NOT RiR tracked Element Parameter reconstruction.

    Caller owns transaction, commit/status handling and rollback of a whole batch.
    """
    DB = _db()
    element = _writable(element)
    parameter = resolve_parameter(element, key)
    if parameter is None: raise ValueError('Parameter not found.')
    if parameter.IsReadOnly: raise ValueError('Parameter is read-only.')
    storage = parameter.StorageType
    if storage == DB.StorageType.Double:
        if isinstance(value, bool) or not isinstance(value, integer_types + (float,)):
            raise TypeError('Expected a finite numeric value.')
        value = float(value)
        if value != value or value in (float('inf'), -float('inf')):
            raise ValueError('Expected a finite numeric value.')
        if unit is not None: value = DB.UnitUtils.ConvertToInternalUnits(value, unit)
    else:
        if unit is not None: raise TypeError('Unit conversion requires a Double parameter.')
        if storage == DB.StorageType.Integer:
            if not isinstance(value, integer_types): raise TypeError('Expected an integer.')
            value = int(value)
        elif storage == DB.StorageType.String:
            if not isinstance(value, (text_type, str)): raise TypeError('Expected text.')
        elif storage == DB.StorageType.ElementId:
            if not isinstance(value, DB.ElementId): raise TypeError('Expected a native ElementId.')
        else: raise TypeError('Unsupported parameter storage type.')
    if not parameter.Set(value): raise ValueError('Revit rejected the parameter value.')
    return element


def set_pinned(element, pinned):
    element = _writable(element)
    if not isinstance(pinned, bool): raise TypeError('Expected a boolean.')
    element.Pinned = pinned
    return element
