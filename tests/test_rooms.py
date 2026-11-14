# Runtime control-flow tests with doubles, not a Revit geometry validation.
import sys

def module(name):
    value = type(sys)(name)
    value.__path__ = []
    sys.modules[name] = value
    return value

class Bag(object):
    def __init__(self, **values):
        self.__dict__.update(values)

class Id(object):
    def __init__(self, value): self.value = value
    def Equals(self, other): return self.value == other.value

class Surface(object):
    def __init__(self, area): self.area, self.disposed = area, False
    def Dispose(self): self.disposed = True

class Room(object):
    def __init__(self, area, phase=2, placed=True):
        self.phase = Id(phase)
        self.Location = object() if placed else None
        self.UniqueId = 'test-room'
        self.surface = None if area is None else Surface(area)
    def get_Parameter(self, name): return Bag(AsElementId=lambda: self.phase)

class Collector(object):
    def __init__(self, document):
        self.document, self.disposed = document, False
        document.collector = self
    def OfCategory(self, category): return self
    def WhereElementIsNotElementType(self): return self.document.rooms
    def Dispose(self): self.disposed = True

class Properties(object):
    @staticmethod
    def Compute(surface):
        if surface.area < 0: return None
        return Bag(Area=surface.area, Centroid=(1, 2, 3), Dispose=lambda: None)

clr = module('clr')
version = Bag(Major=1, Minor=35)
clr.AddReference = lambda name: Bag(GetName=lambda: Bag(Version=version))
module('Autodesk')
module('Autodesk.Revit').DB = Bag(FilteredElementCollector=Collector,
    BuiltInCategory=Bag(OST_Rooms=1), BuiltInParameter=Bag(ROOM_PHASE=2))
module('Rhino').RhinoDoc = Bag(ActiveDoc=Bag(ModelUnitSystem='Meters'))
module('Rhino.Geometry').AreaMassProperties = Properties
module('RhinoInside')
revit = module('RhinoInside.Revit')
revit.Revit = Bag(ActiveDBDocument=None)
module('RhinoInside.Revit.GH')
module('RhinoInside.Revit.GH.Types').RoomElement = lambda room: Bag(TrimmedSurface=room.surface)

scope = {}
with open(sys.argv[1] + '/room_areas.py') as source:
    exec(compile(source.read(), 'room_areas.py', 'exec'), scope)
run = scope['run']
def doc(rooms):
    return Bag(IsValidObject=True, Phases=[Bag(Id=Id(1)), Bag(Id=Id(2))], rooms=rooms)

d = doc([Room(10), Room(2.5), Room(99, phase=1), Room(99, placed=False)])
result = run(d)
assert result['total'] == 12.5
assert result['partials'] == [10.0, 12.5]
assert len(result['rooms']) == 2 and len(result['centroids']) == 2
assert result['outputs']['node3']['Result'] == 12.5
assert result['outputs']['node4'] == 12.5
assert result['unit'] == 'Meters^2' and d.collector.disposed
assert run(doc([]))['total'] == 0.0
assert scope['accumulate']([2, -3, 4]) == (3.0, [2.0, -1.0, 3.0])
for bad_area in [None, -1]:
    valid = Room(5)
    invalid = Room(bad_area)
    d = doc([valid, invalid])
    try: run(d)
    except ValueError: pass
    else: raise AssertionError('Invalid geometry must block the calculation')
    assert d.collector.disposed and valid.surface.disposed
    if invalid.surface is not None: assert invalid.surface.disposed
version.Minor = 36
try: run(doc([]))
except NotImplementedError: pass
else: raise AssertionError('Unsupported RiR version must be rejected')
print('PASS: room runtime scenarios (API doubles; no real Revit geometry)')
