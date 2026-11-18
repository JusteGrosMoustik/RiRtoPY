# RiRtoPY 0.6.0 : preparer une conversion Python mieux documentee

Le rapport par defaut est maintenant une analyse fonctionnelle compacte orientee conversion Python : modules observes, interfaces candidates, dependances, contrats item/list/tree, types et modificateurs, code personnalise et donnees utiles. Un niveau Vue d'ensemble et un niveau Exhaustif completent le JSON detaille.

- Extraction explicite des sources VB/C# historiques, GhPython et RhinoCode, avec adaptateur/version et raisons d'echec.
- Textes des Scribbles, police, coins, groupes sans nom clairement identifies, couleurs et positions du canvas.
- Diagnostics globaux separes entre extraction, messages runtime caches et coherence des connexions.
- Remontee des dependances d'une sortie, y compris a travers les clusters imbriques.
- Resumes geometriques de courbes, surfaces, Breps, maillages et reperes deja disponibles, sans resoudre de references externes.
- Horodatages, versions, etat du fichier et du Solver ; dernier calcul connu uniquement s'il a ete observe par l'outil.
- Comparaison de deux captures JSON : cablage, parametres/scripts, annotations, disposition et valeurs volatiles.
- Chemins masques par defaut, exclusions facultatives des valeurs/expressions et des sources, apercu et confirmation avant export local.

Validation locale : 70 assertions de rapport/analyse/confidentialite et 4 assertions geometriques simples ; compilation plugin/installateur reussie. Lecture native-free de l'archive Rampe : 938 objets hors groupes, 157 Scribbles, sources exactes des 14 scripts, 15 clusters dont 11 ouverts et 4 proteges ; 926 references resolues et une reference archive orpheline connue. Ces controles ne prouvent pas l'execution du lecteur ni les mesures de courbes/surfaces/Breps dans une session Rhino. Aucun Solver ni document Revit n'a ete execute.

Installer apres sauvegarde et fermeture de Rhino/Revit. La conversion Python reste une etape ulterieure, a verifier sur les entrees/sorties du fichier de reference.
