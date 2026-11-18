# RiRtoPY

## Objectif unique : relever un fichier Grasshopper pour ChatGPT

La version 0.5.0 exporte les faits contenus dans **tout le document actif** :

- nom et chemin du fichier Grasshopper ;
- tous les composants, noms, surnoms, types, GUID et groupes (y compris groupes englobants) ;
- tous les clusters accessibles, puis leurs composants, groupes et clusters imbriques, recursivement ;
- toutes les connexions, indices des ports et ordre des sources ;
- correspondances entre ports externes du cluster et points de passage internes, relevees par GUID ;
- textes des Panels, meme connectes, donnees persistantes et donnees volatiles deja disponibles ;
- chemins de branches, ordre des valeurs, valeurs nulles, types et expressions des parametres ;
- valeurs des sliders et toggles, bornes des sliders, choix et expressions des listes de valeurs.

Le composant RiRtoPY lui-meme est exclu du releve. Les groupes restent identifies separement des composants. Les identifiants sont prefixes par le chemin du cluster pour distinguer plusieurs instances contenant les memes GUID internes.

## Utilisation

1. Ouvrir le fichier Grasshopper a analyser.
2. Menu **RiRtoPY > Ouvrir RiRtoPY**, ou double-clic sur son composant.
3. **Analyser le graphe** pour consulter l'apercu complet.
4. **Exporter pour ChatGPT** : le document actif est relu au moment de l'export.
5. Transmettre le fichier Markdown a ChatGPT, et le JSON pour une analyse detaillee.

Deux fichiers sont produits :

- `grasshopper.analysis.md` : releve lisible, organisation par document et cluster, inventaire complet et donnees port par port ;
- `grasshopper.graph.json` : les memes donnees structurees, sans limite volontaire sur le nombre d'elements serialises.

Aucun canevas Python ni demande automatique de reecriture n'est produit. Les textes du fichier sont des donnees de reference, pas des instructions a executer.

## Limites explicites

L'outil ne lance pas le Solver, ne modifie pas le graphe ni la maquette Revit. Les valeurs volatiles correspondent au dernier etat disponible : elles peuvent etre vides ou perimees. L'outil ne charge pas des references externes pour obtenir une valeur manquante et ne pretend pas avoir recalcule le fichier.

Les donnees complexes (geometrie, elements Revit, objets de plugins) sont representees par leur type et leur texte disponible, sans promesse de serialisation geometrique complete. Le code interne des composants scripts n'est pas extrait. Les clusters proteges ne sont pas contournes : leur presence et leur inaccessibilite sont signalees. Les anciens clusters peuvent etre ouverts sans que la correspondance des ports soit disponible ; cela est signale comme incomplet. Une reference cyclique ou une profondeur de securite de 128 documents interrompt uniquement cette branche et signale l'extraction incomplete.

Une erreur de lecture sur un objet ne supprime pas les autres objets du rapport. Les etats `captured`, `empty`, `unavailable`, `partial` et `error` distinguent donnees capturees, vides et illisibles. Aucun contenu n'est volontairement tronque.

## Installer et developper

Fermer Rhino et Revit apres avoir enregistre le travail, puis lancer `RiRtoPY-Setup-0.5.0.exe`. Installation utilisateur dans `%APPDATA%\Grasshopper\Libraries\RiRtoPY`. Garder une seule copie du plugin. Le bouton de mise a jour conserve la verification SHA-256 des releases GitHub. L'EXE n'est pas signe numeriquement.

Avec Rhino 7 installe : `scripts/build.ps1`, puis `scripts/test.ps1`. Le compilateur .NET Framework suffit. `-ReferenceDirectory` permet de fournir RhinoCommon.dll, Grasshopper.dll et GH_IO.dll separement. Le projet cible net48 et C# 5.

Le plugin et l'installateur n'incluent plus les anciens moteurs de traduction ni de bibliotheque Python. Ces sources restent dans le depot comme historique, avec leurs tests dans `scripts/test-legacy.ps1` ; elles ne font pas partie du fonctionnement de l'outil.

Le workflow GitHub compile, teste et publie les nouvelles versions depuis main (installateur, ZIP du plugin et SHA256SUMS.txt). Une release existante n'est jamais remplacee. Voir [Validation](docs/VALIDATION.md) pour les limites des essais et les verifications dans Grasshopper.

References de lecture : [GH_Cluster.Document](https://developer.rhino3d.com/api/gh1/type/grasshopper.kernel.special.gh-cluster/), [GH_Group](https://developer.rhino3d.com/api/gh1/type/grasshopper.kernel.special.gh-group/), [GH_Panel](https://developer.rhino3d.com/api/gh1/type/grasshopper.kernel.special.gh-panel/). Le format `ParamMap` est celui observe dans le SDK Rhino 7 ; toute correspondance absente ou incoherente est signalee.
