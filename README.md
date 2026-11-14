# RiRtoPY

Plugin Windows pour utiliser Grasshopper comme guide de developpement de fonctions Python.

## Version 0.2.0 : prototype scalaire et profil pieces experimental

- Menu **RiRtoPY** dans Grasshopper, et composant **RiRtoPY / Outils** (double-clic).
- Analyse de la selection avec ses dependances en amont, ou de tout le document.
- Rapport JSON : GUID des objets/composants, types CLR, versions des assemblies, connexions, ports et principaux modificateurs.
- Export Python lisible pour Number Slider, Boolean Toggle, Generic Data scalaire et les composants modernes Addition, Subtraction, Multiplication et Division.
- Les constantes exportees sont remplacables par `run(inputs={"InstanceGuid": valeur})`. Les sorties sont indexees par InstanceGuid.
- Bouton **Rechercher une mise a jour** : verification des releases stables, telechargement de l'installateur, controle SHA-256, ouverture de l'installateur.
- Installateur Windows autonome, par utilisateur, sans droits administrateur.

**Limites :** hors du profil experimental decrit dans [docs/ROOM_AREAS.md](docs/ROOM_AREAS.md), cette version ne traduit pas les arbres/listes, clusters, scripts, expressions, panels, geometries, ni les composants Rhino.Inside/Revit. Elle ne modifie pas Revit. Tout objet ou comportement reconnu comme non pris en charge bloque la fonction generee avec `NotImplementedError` ; le rapport reste exportable. Les valeurs d'objets inconnus ne sont pas figees pour simuler une traduction. Ce prototype ne garantit pas l'equivalence de tout graphe Grasshopper.

## Installer

1. Ouvrir [Releases](https://github.com/JusteGrosMoustik/RiRtoPY/releases) et telecharger `RiRtoPY-Setup-0.2.0.exe`.
2. Enregistrer le travail et fermer toutes les instances de Rhino et Revit.
3. Lancer l'installateur et cliquer sur **Installer / Mettre a jour**.
4. Relancer Rhino ou Rhino.Inside.Revit, ouvrir Grasshopper, puis le menu **RiRtoPY**.

Destination : `%APPDATA%\Grasshopper\Libraries\RiRtoPY`. Ne pas installer aussi une copie manuelle du `.gha` ailleurs : un seul exemplaire doit etre charge.

Alternative portable : extraire le ZIP dans ce meme dossier, debloquer le fichier si Windows le demande, puis relancer les logiciels. Ne pas copier les DLL McNeel ou Autodesk.

L'EXE n'est pas signe numeriquement ; Windows peut afficher un avertissement. Verifier l'origine du telechargement. Les empreintes de release sont dans `SHA256SUMS.txt` ; elles detectent une corruption mais ne remplacent pas une signature editeur.

Desinstallation : fermer Rhino/Revit, puis supprimer uniquement le dossier `%APPDATA%\Grasshopper\Libraries\RiRtoPY`. Une ancienne DLL est conservee comme `RiRtoPY.previous.bak` apres remplacement.

## Utiliser

Creer deux Number Sliders et les connecter a un composant Addition. Selectionner Addition, ouvrir RiRtoPY et cliquer sur **Analyser le graphe**. Les deux curseurs sont inclus automatiquement. **Exporter Python + rapport** produit un `.py` et un `.graph.json`.

Le Python exporte correspond a la derniere analyse : refaire l'analyse apres une modification du graphe. Le plugin ne lance pas le Solver pour analyser un document.

## Compatibilite et validation

Compilation contre les API Rhino 7 / Grasshopper 1 et .NET Framework. Cible initiale : Windows, Rhino 7/8/9 et Grasshopper 1 ; chargement dans Rhino.Inside a verifier sur chaque combinaison exacte Rhino/Revit. Ne pas deduire une compatibilite Revit validee du seul succes de compilation.

Les tests automatiques couvrent l'ordonnancement, les graphes incomplets/cycliques, le blocage des semantiques non prises en charge, la generation et l'execution du Python, et la comparaison des versions. Le chargement de l'interface et l'installation doivent aussi etre verifies dans les logiciels.

## Developper et compiler

Avec Rhino 7 installe et Windows PowerShell / PowerShell :

```powershell
.\scripts\build.ps1 -RhinoRoot 'C:\Program Files\Rhino 7'
.\scripts\test.ps1
```

Le compilateur C# .NET Framework fourni par Windows suffit pour cette premiere version. Les livrables sont dans `dist/`. Les chemins locaux ne sont pas stockes dans les sources. Pour utiliser des references de SDK sans Rhino installe, passer `-ReferenceDirectory` contenant RhinoCommon.dll, Grasshopper.dll et GH_IO.dll.

`RiRtoPY.csproj` permet aussi d'ouvrir le projet dans Visual Studio avec le SDK .NET et les outils de developpement .NET Framework 4.8. Les references sont restaurees depuis NuGet.

Pour publier une mise a jour : executer `scripts/set-version.ps1 -Version 0.2.0`, actualiser les notes de release, compiler/tester et pousser sur `main`. Le workflow reconstruit le plugin et son installateur puis cree une release si ce numero n'existe pas encore. Une release existante n'est pas ecrasee.

Pour la premiere publication depuis le paquet complet : installer Git et GitHub CLI, se connecter avec `gh auth login`, puis executer `Publish-GitHub.ps1`. Ce script verifie les empreintes locales, clone le depot dans un nouveau dossier temporaire, copie les sources, cree un commit et pousse sur main sans force. Il joint aussi les binaires deja compiles a la release. Il conserve sa copie de travail et ne modifie aucun autre depot local.

Architecture : `src/GraphReader.cs` lit le document, `src/Models.cs` decrit le graphe, `src/PythonEmitter.cs` traduit le sous-ensemble supporte, `src/Plugin.cs` gere l'interface, `src/UpdateService.cs` gere les releases. `installer/Setup.cs` contient l'installation.

Les nouvelles traductions doivent etre ajoutees avec un exemple et une verification de resultat. Prochain objectif : operations Revit de lecture, puis gestion explicite des transactions et des elements crees.
