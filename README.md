# RiRtoPY

Utiliser Grasshopper comme brouillon visuel pour extraire rapidement les fonctionnalites d'un outil et les recoder en Python avec ChatGPT.

## Version 0.4.0 : extraction pour ChatGPT

L'objectif est de comprendre et reorganiser la logique metier. La priorite est un dossier de reecriture exploitable, meme si les composants ne sont pas traduits automatiquement.

1. Selectionner un bloc Grasshopper, ou choisir tout le document dans RiRtoPY.
2. Ouvrir le menu RiRtoPY ou double-cliquer sur son composant, puis Analyser le graphe.
3. Cliquer sur Exporter pour ChatGPT.
4. Donner d'abord le fichier `.brief.md` a ChatGPT, avec le resultat souhaite. Ajouter le JSON pour approfondir une partie.

Trois fichiers sont produits :

- `.brief.md` : demande de reecriture prete, vue d'ensemble, grandes familles de fonctions, entrees, constantes, noms de groupes/libelles, sorties observees, flux entre familles et informations a completer.
- `.sketch.py` : canevas de conception a reorganiser en fonctions metier avec ChatGPT. Il ne calcule aucun resultat et ne modifie pas Revit.
- `.graph.json` : annexe detaillee avec objets, ports, connexions, GUID et valeurs effectivement capturees.

L'analyse ne lance pas le Solver. Les composants inconnus, clusters, listes et arbres sont documentes sans bloquer le dossier. Les familles sont des regroupements thematiques, pas une preuve d'ordre d'execution ou d'intention utilisateur. L'interieur des clusters et le code des scripts ne sont pas extrapoles. Les textes du graphe sont des donnees de reference, pas des instructions a executer.

Les nouvelles analyses capturent les descriptions des objets, leurs groupes directs et le texte des Panels non connectes. Les anciens rapports restent utilisables pour l'extraction, avec leurs informations manquantes signalees. Aucun fichier Grasshopper complet n'est exige pour commencer a travailler a partir d'un rapport existant.

## Installer

Telecharger [RiRtoPY 0.4.0](https://github.com/JusteGrosMoustik/RiRtoPY/releases/tag/v0.4.0), enregistrer le travail, fermer Rhino et Revit, lancer l'installateur puis relancer les logiciels. Installation par utilisateur sans droits administrateur dans `%APPDATA%\Grasshopper\Libraries\RiRtoPY`. Garder une seule copie du plugin.

Le bouton Rechercher une mise a jour utilise les releases GitHub publiques et verifie SHA-256 avant d'ouvrir l'installateur. L'EXE est non signe numeriquement. Le ZIP du plugin permet une installation manuelle ; debloquer le fichier si Windows le demande. Ne pas redistribuer/copier les DLL McNeel ou Autodesk.

## Ressources pour la reecriture

Les traductions experimentales precedentes restent dans les sources comme ressources de developpement. Elles ne conditionnent plus l'export de l'interface :

- [Bibliotheque Python et limites](docs/GENERAL_NODES.md).
- [Catalogue de 381 composants RiR, sources officielles et statut](python/node-catalog.json).
- [Ancien profil de calcul des surfaces de pieces](docs/ROOM_AREAS.md).
- [Validation et essais manuels restants](docs/VALIDATION.md).

Les exemples d'export executable dans les anciens guides concernent les moteurs experimentaux, pas le nouveau bouton d'extraction. Les fonctions Python aident a recoder ; leur disponibilite ne signifie pas que tout graphe est automatiquement converti.

## Developper

Avec Rhino 7 installe : `scripts/build.ps1` puis `scripts/test.ps1`. Le compilateur .NET Framework Windows suffit. Pour compiler sans Rhino installe, passer `-ReferenceDirectory` avec RhinoCommon.dll, Grasshopper.dll et GH_IO.dll. Le projet Visual Studio vise net48.

`FunctionalBriefEmitter.cs` produit le dossier et le canevas, `GraphReader.cs` collecte les informations, `Models.cs` definit le rapport, `Plugin.cs` gere l'interface. Les moteurs precedents PythonEmitter/GeneralRevitEmitter/RoomAreaEmitter et leurs tests sont conserves pour la recherche de traductions et la regression.

Le workflow GitHub compile, teste et publie une nouvelle version depuis main, sans remplacer une release existante. Les releases contiennent installateur, ZIP du plugin, bibliotheque Python et SHA256SUMS.txt.

References de capture : [GH_Group.ObjectIDs](https://developer.rhino3d.com/api/gh1/type/grasshopper.kernel.special.gh-group/), [GH_Panel.UserText](https://developer.rhino3d.com/api/gh1/type/grasshopper.kernel.special.gh-panel/).
