# Validation 0.1.0

Effectue sur Windows, avec les references Rhino 7 installees :

- Compilation du plugin .gha : reussie.
- Compilation de l'installateur EXE avec le plugin integre : reussie.
- Moteur C# et gestion des versions : 32 assertions reussies.
- Python genere execute sous IronPython 2.7 : 6 assertions reussies.

Non effectue : chargement de l'interface dans une session Rhino/Grasshopper, installation sur un second poste, conversion d'un graphe reel, verification d'une mise a jour depuis une release publiee, validation Rhino.Inside/Revit. Le workflow GitHub n'a pas encore ete execute.

Essai manuel conseille : deux Number Sliders + Addition, analyser Addition et exporter. Comparer le resultat de `run()` avec Grasshopper. Verifier ensuite qu'un composant Revit ou un Flatten provoque un blocage explicite. Tester l'installation et les remplacements apres fermeture des logiciels. Le bouton de mise a jour d'une version 0.1.0 ne telecharge que les releases stables strictement superieures.

Reprise du 14 novembre 2026 : compilation locale et tests relances avec succes. Corrections : chaines Unicode compatibles IronPython 2/Python 3, validation des operations et du nombre d'entrees, blocage des expressions sur les parametres autonomes. Le depot distant initial ne contenait que son titre README ; historique conserve.
