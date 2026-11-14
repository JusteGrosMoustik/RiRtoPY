# Profil experimental : surfaces des pieces

Chaine prise en charge en 0.2.0 : `Query Rooms -> Surface -> Area -> Mass Addition -> Panel`, cinq objets sans autre branche, avec un fil par entree. Selectionner les cinq objets ou uniquement le Panel (dependances incluses).

Le profil est volontairement strict : RiR GH 1.35, les cinq entrees Point/Placed/Number/Name/Level presentes, toutes optionnelles et non connectees ; seule Placed a une valeur persistante, True. Aucun Flatten, Graft, Reverse, Simplify, expression, phase explicite, filtre, document lie, connexion Centroid ou sortie Partial Results n'est accepte. Les listes sont gerees dans cette seule chaine, pas comme un moteur general d'arbres Grasshopper.

Installer 0.2.0 puis refaire Analyser et Exporter. Le rapport schema 2 enregistre Optional et HasPersistentData : l'ancien rapport ne permettait pas de distinguer une entree absente d'une valeur non scalaire. Aucun ancien rapport n'est automatiquement presume compatible.

## Executer l'export

Executer le fichier dans un environnement Python IronPython 2.7 ou Python 3 disposant de clr, au sein de Rhino.Inside.Revit, sur le thread de l'API Revit. Le plugin exporte le fichier mais ne l'execute pas. Les assemblies Rhino, Revit et RhinoInside.Revit.GH 1.35 doivent deja etre disponibles. Aucun graphe Grasshopper n'est execute. Le script conserve une dependance a la bibliotheque de types RiR GH pour reproduire exactement la conversion Surface.

Appeler `result = run()` pour le document Revit actif, ou `result = run(document)` avec un document Revit valide. Lire `result['total']`, `result['areas']`, `result['centroids']`, `result['rooms']`, `result['surfaces']`, `result['partials']` et `result['unit']`. `result['outputs']` indexe les objets selectionnes par leur InstanceGuid ; les sorties Area et Mass Addition sont des dictionnaires par nom de port. Le Panel terminal expose la valeur numerique a afficher, pas son texte formate. Ces listes correspondent a cette chaine simple ; aucun chemin d'arbre general n'est exporte.

Le calcul ne cree ni ne modifie d'element Revit et ne demarre aucune transaction. La derniere phase du document est utilisee comme dans Query Rooms 1.35 lorsque Phase est absent. Les pieces non placees sont exclues. Les surfaces sont les Breps rognes de `RoomElement.TrimmedSurface` (contours, trous et hauteur de calcul selon RiR), et non le parametre Revit ROOM_AREA. Les aires sont en unites du modele Rhino au carre, pas forcement en m2. Une surface absente ou un calcul d'aire invalide interrompt le calcul : aucune somme partielle n'est presentee comme complete.

Les Breps retournes appartiennent a l'appelant : appeler `surface.Dispose()` sur chacun apres utilisation. Les proprietes de masse et le collecteur Revit sont liberes par le script.

## Verification manuelle requise

Dans le meme document et les memes unites, comparer le Panel Grasshopper (124.304517 sur la capture fournie) a `run()['total']` avec une tolerance adaptee, puis comparer le nombre et les aires des pieces individuellement. Verifier une piece avec trou, une piece non placee, une piece non fermee et un projet a plusieurs phases. Le nombre affiche sur la capture n'est pas une valeur validee automatiquement : le fichier Revit n'est pas fourni.

Les tests Python de controle emploient des doublures d'API ; ils ne prouvent ni l'equivalence geometrique ni la compatibilite dans Revit. Les fixtures de tests sont synthetiques ; les documents fournis par l'utilisateur ne sont pas publies.

## Sources API consultees

Revision RiR correspondant a l'archive fournie : `faa6a97653e9da17c6779103ac39537efff60da8`.

- [QuerySpatialElements : filtres, phase implicite, placement](https://github.com/mcneel/rhino.inside-revit/blob/faa6a97653e9da17c6779103ac39537efff60da8/src/RhinoInside.Revit.GH/Components/Topology/QuerySpatialElements.cs)
- [GraphicalElement : conversion GH_Surface](https://github.com/mcneel/rhino.inside-revit/blob/faa6a97653e9da17c6779103ac39537efff60da8/src/RhinoInside.Revit.GH/Types/GraphicalElement.cs)
- [SpatialElement : TrimmedSurface et contours](https://github.com/mcneel/rhino.inside-revit/blob/faa6a97653e9da17c6779103ac39537efff60da8/src/RhinoInside.Revit.GH/Types/Topology/SpatialElement.cs)
- [Autodesk : transactions Revit](https://help.autodesk.com/cloudhelp/2024/ENU/Revit-API/files/Revit_API_Developers_Guide/Basic_Interaction_with_Revit_Elements/Revit_API_Developers_Guide_Basic_Interaction_with_Revit_Elements_Transactions_html.html)
