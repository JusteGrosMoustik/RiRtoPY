# Validation 0.2.0

Reprise du 14 novembre 2026.

- Compilation locale du plugin et de l'installateur contre Rhino 7 : reussie.
- Tests : 32 assertions C# scalaires, 14 assertions C# du profil pieces, 6 assertions Python scalaires sous IronPython 2.7 : reussis.
- Scenarios Python du profil avec doublures API : phase implicite, exclusion des pieces non placees, somme et sommes cumulees, sorties, liste vide, geometrie manquante/invalide, liberation des ressources, rejet d'une version RiR incompatible : reussis. Ils ne valident pas le calcul geometrique reel.
- Graphe utilisateur de cinq objets : profil reconnu apres verification des metadonnees optionnelles/persistantes dans son archive XML. Script produit ; pas d'execution Revit effectuee. Les fichiers utilisateur restent hors du depot.
- Capture utilisateur : interface 0.1.0 chargee dans Grasshopper / Rhino 9.0.26293.12303 avec RhinoInside.Revit.GH 1.35.9698.15636 ; analyse et blocage explicite observes. Cela ne valide pas l'interface ni l'export Revit 0.2.0.
- Release 0.1.0 : workflow GitHub reussi, installateur et ZIP telecharges et controles SHA-256 lors de la session precedente.

A valider manuellement : installation 0.2.0, chargement reel de cette version, execution du script dans Rhino.Inside.Revit, comparaison geometrique avec le graphe et son Panel, mise a jour complete depuis 0.1.0. Voir ROOM_AREAS.md pour le protocole et les limites.

Le workflow Windows compile, teste sous Python 3 et publie chaque nouvelle version. Consulter Actions pour son resultat effectif.

## Validation du lot general 0.3.0

- Compilation locale du plugin/EXE et paquet Python : reussie.
- 58 assertions C# (32 + 14 + 12), 6 assertions Python scalaires, scenarios pieces et 49 assertions Python du lot general sous IronPython 2.7 : reussis.
- Les controles des appels Revit utilisent des doublures ; aucune nouvelle equivalence fonctionnelle n'est validee dans Revit.
- Catalogue derive de 317 fichiers officiels de la revision RiR faa6a97653e9da17c6779103ac39537efff60da8, sans redistribution de ces fichiers de reference.
- Regression du profil pieces et du profil scalaire incluse. Les documents utilisateur restent locaux.
- A tester dans Rhino.Inside.Revit : filtre Categories -> Query Elements (avec/sans Limit) -> Element Name/Category, valeurs natives externes, identifiants 64 bits, unites de parametres et lecture des materiaux. Ne pas lancer les primitives manuelles d'ecriture hors d'une transaction controlee par l'appelant.
