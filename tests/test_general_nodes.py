# Control-flow/API contract doubles only. No native Revit validation claimed.
import sys
checks = 0
def check(condition):
    global checks
    checks += 1
    assert condition
def rejects(kind, function, *args):
    global checks
    checks += 1
    try: function(*args)
    except kind: return
    raise AssertionError('Expected ' + str(kind))
class Bag(object):
    def __init__(self, **kwargs): self.__dict__.update(kwargs)
def module(name):
    value=type(sys)(name); value.__path__=[]; sys.modules[name]=value; return value
class Id(object):
    def __init__(self, value): self.Value=value
class Category(object):
    def __init__(self, identifier): self.Id=Id(identifier)
class Filter(object):
    def __init__(self, ids, inverted=False): self.ids, self.inverted=ids, inverted
    def accepts(self, element): return (element.Category.Id.Value in [i.Value for i in self.ids]) != self.inverted
class GenericList(object):
    def __getitem__(self, kind): return NativeList
class NativeList(list):
    def Add(self, value): self.append(value)
class Element(object):
    def __init__(self, identifier, category=1, is_type=False):
        self.Id=Id(identifier); self.UniqueId='uid-'+str(identifier); self.Category=Category(category)
        self.IsType=is_type; self.IsValidObject=True; self.Pinned=False; self.Name='native'; self.ViewSpecific=False
        self.Parameters=[]
    def GetParameters(self, key): return [p for p in self.Parameters if p.Definition.Name==key]
    def get_Parameter(self, key): return self.Parameters[0] if self.Parameters else None
    def GetTypeId(self): return Id(999)
class Collector(object):
    def __init__(self, doc): self.doc=doc; self.values=list(doc.elements); self.disposed=False; doc.last_collector=self
    def WhereElementIsNotElementType(self): self.values=[v for v in self.values if not v.IsType]; return self
    def WherePasses(self, f): self.values=[v for v in self.values if f.accepts(v)]; return self
    def GetElementCount(self): return len(self.values)
    def __iter__(self): return iter(self.values)
    def Dispose(self): self.disposed=True
class Parameter(object):
    def __init__(self, storage, value):
        self.StorageType=storage; self.value=value; self.HasValue=True; self.IsReadOnly=False; self.Definition=Bag(Name='Size')
    def AsDouble(self): return self.value
    def AsInteger(self): return self.value
    def AsString(self): return self.value
    def AsElementId(self): return self.value
    def AsValueString(self): return 'formatted'
    def Set(self, value): self.value=value; return True
DB=Bag(ElementId=Id, Category=Category, ElementFilter=Filter, ElementMulticategoryFilter=Filter,
       FilteredElementCollector=Collector, StorageType=Bag(Double=1,Integer=2,String=3,ElementId=4),
       UnitUtils=Bag(ConvertFromInternalUnits=lambda value, unit: value*unit,
                     ConvertToInternalUnits=lambda value, unit: value/unit),
       ElementIsElementTypeFilter=lambda inverted: inverted,
       LogicalAndFilter=lambda values: ('and',values), LogicalOrFilter=lambda values: ('or',values),
       FilterRule=object, ElementParameterFilter=lambda rules,inverted: ('rules',rules,inverted))
module('clr').AddReference=lambda name: None
module('Autodesk'); module('Autodesk.Revit').DB=DB
module('System'); module('System.Collections'); module('System.Collections.Generic').List=GenericList()
module('RhinoInside'); revit=module('RhinoInside.Revit'); module('RhinoInside.Revit.GH')
module('RhinoInside.Revit.GH.Types').Element=Bag(FromElement=lambda element: Bag(Nomen='logical-'+str(element.Id.Value)))
doc=Bag(IsValidObject=True,IsReadOnly=False,IsModifiable=False,elements=[Element(1),Element(2),Element(3),Element(4,2),Element(5,1,True)])
for e in doc.elements: e.Document=doc
doc.GetElement=lambda identifier: next((e for e in doc.elements if (e.UniqueId==identifier if isinstance(identifier,str) else e.Id.Value==identifier.Value)),None)
revit.Revit=Bag(ActiveDBDocument=doc)
scope={}
with open(sys.argv[1]+'/general_revit.py') as source:
    exec(compile(source.read(),'general_revit.py','exec'),scope)
result=scope['run']({'categories':[Category(1)]},doc)
active=Bag(IsValidObject=True)
revit.Revit.ActiveDBDocument=active
check(scope['run']({'categories':[Category(1)]},doc)['active']['Active Document'] is active)
revit.Revit.ActiveDBDocument=doc
check(result['query']['Count']==3)
check(len(result['query']['Elements'])==1)
check(result['name']['Name']==['logical-1'])
check(result['category']['Category'][0].Id.Value==1)
check(doc.last_collector.disposed)
rejects(KeyError,scope['run'],{},doc)
f=scope['category_filter']([Category(1)])
check(scope['query_elements'](f,doc,0)=={'Elements':[],'Count':3})
check(len(scope['query_elements'](f,doc)['Elements'])==3)
rejects(ValueError,scope['query_elements'],f,doc,-1)
rejects(ValueError,scope['query_elements'],f,doc,True)
rejects(ValueError,scope['query_elements'],None,doc)
check(scope['exclude_element_types_filter']() is True)
check(scope['exclude_element_types_filter'](True) is False)
check(scope['active_document']() is doc)
check(scope['query_element'](1,doc) is doc.elements[0])
check(scope['query_element']('uid-2',doc) is doc.elements[1])
rejects(TypeError,scope['query_element'],True,doc)
check(scope['element_id_value'](Id(2**40))==2**40)
check(scope['element_id_value'](Bag(IntegerValue=7))==7)
check(scope['map_element'](lambda e: {'x':e},[],['x'])=={'x':[]})
rejects(NotImplementedError,scope['map_element'],lambda e: e,[[1]],['x'])
rejects(NotImplementedError,scope['scalar_input'],[1])
rejects(ValueError,scope['category_filter'],[])
check(scope['logical_and']([f]) is f)
check(scope['logical_or']([None,f]) is f)
check(scope['logical_and']([f,f])[0]=='and')
rejects(ValueError,scope['logical_or'],[])
rejects(ValueError,scope['parameter_filter'],[])
e=doc.elements[0]; p=Parameter(1,2.5); e.Parameters=[p]
check(scope['element_parameter'](e,'Size')==2.5)
check(scope['element_parameter'](e,'Size',10)==25)
check(scope['parameter_text'](p)=='formatted')
p.HasValue=False; check(scope['parameter_value'](p) is None); p.HasValue=True
e.Parameters.append(Parameter(1,9)); rejects(ValueError,scope['resolve_parameter'],e,'Size'); e.Parameters=[p]
rejects(RuntimeError,scope['set_parameter'],e,'Size',5.0)
doc.IsModifiable=True
scope['set_parameter'](e,'Size',50.0,10); check(p.value==5)
rejects(ValueError,scope['set_parameter'],e,'Size',float('nan'))
rejects(TypeError,scope['set_parameter'],e,'Size',True)
p.IsReadOnly=True; rejects(ValueError,scope['set_parameter'],e,'Size',3); p.IsReadOnly=False
p.StorageType=2; scope['set_parameter'](e,'Size',3); check(p.value==3)
rejects(TypeError,scope['set_parameter'],e,'Size',3.5)
rejects(TypeError,scope['parameter_value'],p,10)
p.StorageType=3; scope['set_parameter'](e,'Size','hello'); check(p.value=='hello')
rejects(TypeError,scope['set_parameter'],e,'Size',3)
p.StorageType=4; scope['set_parameter'](e,'Size',Id(3)); check(scope['parameter_value'](p).Value==3)
rejects(TypeError,scope['set_parameter'],e,'Size',3)
scope['set_pinned'](e,True); check(scope['element_pinned'](e) is True)
rejects(TypeError,scope['set_pinned'],e,1)
doc.IsReadOnly=True; rejects(RuntimeError,scope['set_pinned'],e,False)
print('PASS: %s assertions Python general nodes (API doubles)' % checks)
