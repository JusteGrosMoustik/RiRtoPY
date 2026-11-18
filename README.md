# RiRtoPY

## Objectif : preparer une conversion Grasshopper vers Python fidele et efficace

La version 0.6.0 rassemble ce dont ChatGPT/Codex a besoin pour comprendre puis transcrire un fichier Grasshopper en Python : algorithmes personnalises, interfaces des modules, contrats des ports, donnees, connexions, annotations, geometrie et limites connues.

Le dossier doit permettre de produire des fonctions Python avec des entrees/sorties explicites et de retrouver les composants sources. Il ne pretend pas que des resultats volatiles non recalculés valident une conversion. L'hote Python cible (Rhino, Rhino.Inside/Revit ou autonome), les unites et les API restent a confirmer avant la transcription.

## Utilisation

1. Ouvrir le fichier Grasshopper, puis **RiRtoPY > Ouvrir RiRtoPY**.
2. **Analyser le graphe**. L'outil lit tout le document et descend dans les clusters accessibles, sans lancer le Solver.
3. Choisir le rapport : **Vue d'ensemble**, **Analyse fonctionnelle** (par defaut), ou **Exhaustif**.
4. Pour isoler un calcul, choisir une sortie dans **Perimetre**. L'outil remonte ses dependances, y compris a travers les points de passage des clusters. Les dependances internes d'un composant restent conservatrices : chaque entree peut influencer chaque sortie.
5. Choisir les filtres de confidentialite et relire l'apercu. **Masquer les chemins** est active par defaut ; **Exclure les valeurs et expressions** et **Exclure les codes sources** sont facultatifs.
6. **Exporter pour ChatGPT** relit le document actif, affiche une confirmation des filtres puis ecrit localement trois fichiers. Aucun envoi automatique a un service externe.

| Fichier | Utilisation pour la conversion |
|---|---|
| `grasshopper.overview.md` | Bilan court, modules observes, entrees/sorties candidates, plugins et principales alertes |
| `grasshopper.analysis.md` | Rapport du niveau choisi ; par defaut interfaces des futures fonctions Python, annotations, extraits des scripts, parametres et connexions compactes |
| `grasshopper.graph.json` | Detail structure complet dans le perimetre et selon les filtres choisis : sources integrales autorisees, ports, arbres, geometrie et diagnostics |

Donner d'abord les deux Markdown a ChatGPT/Codex. Ajouter le JSON pour transcrire ou verifier un module precis. Les reperes `Nxxxx` et identifiants scopes permettent de retrouver les objets d'origine. Les extraits raccourcis sont explicitement marques ; le JSON conserve leur detail autorise. Aucun ancien canevas de conversion automatique n'est ajoute.

## Contenu de la capture

- Nom et chemin du fichier, versions Rhino/Grasshopper, horodatages de debut/fin, etat modifie/enregistre et etat du Solver observe.
- Composants, noms, surnoms, GUID, positions et dimensions du canvas.
- Groupes : nom brut, identifiant, couleur ARGB, bordure, limites et membres. Un groupe sans nom garde son nom brut vide et recoit un libelle technique stable ; aucune intention n'est inventee.
- Scribbles : texte exact, police et quatre coins. Panels : texte, donnees persistantes et dernier contenu volatile disponible, meme connectes.
- Sliders, toggles et listes de valeurs ; contrats des ports, hints de types, acces item/list/tree, expressions, graft/flatten, reverse, simplify et desactivation.
- Clusters accessibles et imbriques, identifiants prefixes par instance, connexions avec indices et ordre des sources, correspondances externes/internes relevees par GUID.
- Scripts VB.NET/C# anciens : code principal, imports, code additionnel et references via proprietes connues ou archive de composant. GhPython : source embarquee et indication du code fourni par entree. RhinoCode recent : texte et langage via l'interface exposee. Chaque script a un adaptateur, une version, un statut et une raison si la source manque.
- Geometrie disponible : courbes (longueur, domaine, extremites, tangentes, fermeture, rayon d'arc si reconnu), surfaces (domaines UV, taille estimee, aire, normale au milieu UV), Breps (validite, faces, aretes, fermeture/solidite, orientation, aire, volume si solide), maillages (comptages, fermeture, aire), points, vecteurs, lignes, arcs et reperes. Bounding box mondiale XYZ pour les objets Rhino disponibles. Les mesures utilisent les unites natives Rhino, indiquees dans la capture.
- Diagnostics separes : `extraction`, `runtime`, `coherence`. La synthese globale remonte les limites des clusters internes. Une erreur de calcul Grasshopper n'est pas assimilee a un echec de lecture.

## Comparer deux etats

**Comparer deux captures JSON** demande le fichier avant puis le fichier apres, affiche et permet d'enregistrer une comparaison Markdown. Elle distingue ajouts/suppressions, cablage, parametres/code/annotations, organisation du canvas et instantanes volatils. La correspondance utilise les identifiants scopes : des GUID regeneres ou des perimetres/filtres differents limitent l'interpretation. Une difference de valeurs volatiles ne prouve pas qu'un nouveau calcul a eu lieu. Les captures 0.5.0 sont lisibles ; les informations absentes ne sont pas recuperees artificiellement.

## Limites et confidentialite

Les modules et interfaces proposes sont des regroupements structurels derives des groupes, clusters et fils. Ils doivent etre confirmes avant d'en faire des fonctions Python. L'outil n'invente pas les formules d'un script absent ou le contenu d'un cluster protege.

Les scripts externes references par chemin ne sont pas ouverts, les protections de cluster ne sont pas contournees, les geometries referencees non chargees ne sont pas resolues. Les geometries sont resumees, pas serialisables a l'identique depuis ce seul dossier. La normale d'une surface est un echantillon au milieu UV ; ces resumes ne prouvent pas a eux seuls la continuite, l'absence de collision ou la conformite d'une rampe.

Les donnees volatiles et messages runtime sont les derniers caches accessibles. Le dernier calcul UTC est inconnu au premier contact ; l'outil observe ensuite les fins de solution sans les provoquer. Un changement d'etat du Solver pendant la lecture signale une capture non atomique. `Complete` concerne les erreurs d'extraction et de coherence dans le perimetre, pas la validite du calcul ni de la future conversion Python. Les avertissements de geometrie restent visibles.

Le masquage parcourt aussi scripts, references et textes, mais ne garantit pas l'anonymat : noms et annotations restent visibles. Il peut remplacer toute la fin d'une ligne contenant un chemin. Les sources ainsi masquees ne sont plus des copies executables exactes ; les filtres et omissions sont annonces dans le rapport. L'exclusion des valeurs supprime aussi les expressions et reglages ; le code conserve peut contenir ses propres constantes tant que l'exclusion des sources n'est pas cochee. Relire avant partage.

Une reference cyclique ou une profondeur de 128 documents interrompt cette branche avec un diagnostic. Les autres branches restent exportables. Les representations textuelles d'objets complexes restent typees et leurs limites signalees.

## Installation et developpement

Fermer Rhino/Revit apres sauvegarde, puis lancer `RiRtoPY-Setup-0.6.0.exe`. Installation utilisateur dans `%APPDATA%\Grasshopper\Libraries\RiRtoPY`. Le bouton de mise a jour utilise les releases GitHub et verifie SHA-256 ; l'EXE n'est pas signe numeriquement. Garder une seule copie du plugin.

```
scripts/build.ps1
scripts/test.ps1
scripts/test-geometry.ps1
scripts/test-reference.ps1 -ReferenceFile 'chemin\Rampe1.61.gh' -RampeBaseline
```

Le build et le test geometrique acceptent `-ReferenceDirectory` pour les SDK RhinoCommon.dll, Grasshopper.dll et GH_IO.dll. Projet net48/C# 5. Les anciens moteurs de traduction restent uniquement dans les sources historiques, hors distribution. Voir [Validation](docs/VALIDATION.md) pour les preuves et essais restant a faire dans Grasshopper.

Le fichier de reference et ses donnees de travail restent hors Git (`work/`). Les releases ne contiennent aucun document utilisateur.
