# Compatible with Python 3 and IronPython 2.7.
import sys

folder = sys.argv[1]
checks = 0
for name, expected in [('division', 3.5), ('addition', 9.0), ('subtraction', 5.0), ('multiplication', 14.0)]:
    scope = {}
    with open(folder + '/' + name + '.py', 'r') as source:
        exec(compile(source.read(), name + '.py', 'exec'), scope)
    assert scope['run']()['result'] == expected, name
    checks += 1
    if name == 'division':
        assert scope['run']({'a': 9})['result'] == 4.5
        checks += 1
scope = {}
with open(folder + '/blocked.py', 'r') as source:
    exec(compile(source.read(), 'blocked.py', 'exec'), scope)
try:
    scope['run']()
except NotImplementedError:
    checks += 1
else:
    raise AssertionError('Unsupported graph must not execute')
print('PASS: %s assertions Python' % checks)
