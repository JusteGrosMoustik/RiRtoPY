# Traductions generales RiR : premier lot

Base documentaire : sources officielles McNeel RiR 1.35, revision `faa6a97653e9da17c6779103ac39537efff60da8`, et API Autodesk. L'inventaire couvre 317 fichiers et 381 composants concrets possedant un ComponentGuid. Les parametres Grasshopper, helpers et classes abstraites ne sont pas tous des composants de cet inventaire.

## Disponibilite

`python/node-catalog.json` conserve pour chaque composant son type CLR, son GUID, sa source officielle et son statut. Aucun composant simplement inventorie n'est considere comme traduit.

- `graph_subset` : 12 composants avec generation Python automatique dans le sous-ensemble ci-dessous.
- `manual_helper_subset` : 15 composants avec fonction Python appelee manuellement, avec restrictions.
- `room_chain_only` : Query Rooms dans le profil de cinq objets documente dans ROOM_AREAS.md.
- `not_translated` : 353 composants restent explicitement non traduits.

## Generation automatique dans le plugin

Composants : Active Document, Query Element, Query Elements, Element Category, Element Name (lecture), Pin Element (lecture), Element View, Category Filter, Exclude Element Types Filter, Parameter Filter (regles natives fournies), Intersection Filter et Union Filter.

Signature CLR + GUID controles pour RiR 1.35, rapport schema 2 obligatoire. Reanalyser avec RiRtoPY 0.3.0. Les entrees optionnelles absentes sont distinguees des donnees persistantes non scalaires. Une entree obligatoire sans source/valeur reste bloquee ; il faut lui connecter une source explicite.

Les parametres sources natifs Element, Category, Element Filter, Built-In Categories ainsi que Generic Data et Panels non connectes peuvent devenir des arguments externes explicites : le script lit `inputs['InstanceGuid']`. Leur contenu n'est jamais invente ni presume recupere du rapport. Fournir les objets natifs DB correspondants, par exemple une liste de DB.Category pour le picker Categories. Un Panel non connecte devient lui aussi un argument explicite, pas une traduction de son texte omis. Une cle absente leve KeyError.

Le script genere est autonome : la bibliotheque de fonctions est incorporee au .py. `run(inputs=None, document=None)` retourne un dictionnaire par InstanceGuid selectionne, puis par nom de port. Les dependances amont sont calculees. Le document fourni est utilise par les requetes sans entree Document ; sinon le document actif est utilise.

Les accesseurs Element Category / Name / View / Pinned acceptent un element ou une liste plate. Leurs sorties conservent cette liste unique. Une liste imbriquee, un arbre ou plusieurs listes item a apparier sont refuses ; aucune semantique generale de branches, Graft, Flatten, Cross Reference ou Longest List n'est revendiquee. Les composants arithmetiques mixtes, clusters et scripts restent bloques dans ce profil. Le profil scalaire independant et le profil pieces restent disponibles.

Element Name emploie `RhinoInside.Revit.GH.Types.Element.FromElement(e).Nomen`, pas simplement `e.Name`. Les entrees Name/Pinned d'ecriture doivent rester absentes ou optionnelles vides. Query Elements exclut les types, exige un filtre et retourne le nombre complet de correspondances meme avec Limit=0 ou une autre limite. Une entree Limit retiree signifie aucune limite ; une entree presente conserve sa valeur persistante.

## Fonctions appelables manuellement

Dans un environnement Python au sein de Revit, sur son thread API, ajouter le dossier python a sys.path puis importer rir_nodes. IronPython 2.7 et Python 3 avec pythonnet vises ; aucun appel Revit n'est fait a l'import.

| Famille | Fonctions |
| --- | --- |
| Document / identifiants | active_document, document_or_active, element_id_value, query_element |
| Requetes | query_elements, query_by_class, query_levels, query_sheets, query_materials, query_phases, query_view_elements, query_title_blocks |
| Elements | element_category, element_type, element_name, element_identity, element_owner_view, element_pinned |
| Filtres | category_filter, class_filter, exclude_element_types_filter, logical_and, logical_or, parameter_filter |
| Parametres | element_parameters, resolve_parameter, parameter_value, parameter_text, element_parameter, set_parameter |
| Materiaux | element_materials, material_quantities |
| Autres | category_by_id, set_pinned |

Exemple, dans Revit :

```python
from Autodesk.Revit import DB
import rir_nodes as r
walls = r.query_elements(r.category_filter([DB.BuiltInCategory.OST_Walls]))
print(walls['Count'])
names = [r.element_name(e) for e in walls['Elements']]
```

Les fonctions manuelles renvoient des objets DB natifs. Elles ne reproduisent pas automatiquement les arbres ni tous les filtres textuels de RiR. Query Levels est le sous-ensemble sans motifs textuels, elevation structurelle ou filtre Building Story ; tri explicite par elevation puis ID. Query Sheets inclut les placeholders et n'applique pas les filtres de texte de RiR. Query View Elements valide la vue mais ne traite pas les liens. Query Element accepte ElementId, entier ou UniqueId, sans References, categorie ou cle de parametre. Element Identity est l'ancien composant avec nom natif ; Element Name est l'accesseur logique RiR.

Parametres : utiliser une cle BuiltInParameter, un GUID partage ou une Definition native. Une chaine est un nom exact localise ; plusieurs correspondances provoquent une erreur. Les doubles sont en unites internes Revit tant qu'aucune unite explicite n'est passee. La conversion utilise UnitUtils ; les booleens Yes/No, enumerations et references ElementId ne sont pas automatiquement convertis en Goo RiR. Parameter Text est l'affichage formate de Revit. Les quantites de materiaux sont en ft2/ft3 ; la peinture n'a pas de volume dans ce helper. Ces restrictions empechent de presenter les fonctions comme l'equivalent integral de Element Parameter ou Material Quantities.

`set_parameter` et `set_pinned` sont des primitives manuelles : elles exigent une transaction deja ouverte par l'appelant et un document non read-only. Elles n'ouvrent, ne valident et n'annulent aucune transaction. L'appelant doit traiter Commit, ses statuts et le rollback d'un lot. Elles ne sont jamais appelees par les graphes generes. Aucun suivi des elements crees, reconstruction d'element, remplacement de type, creation de vue/feuille ni suppression n'est active.

## Validation

Tests du moteur C#, execution Python 2.7 locale et Python 3 en CI. Les tests des appels Revit utilisent des doublures API ; ils ne prouvent pas le fonctionnement reel dans Revit, ni la fidelite geometrique. Il reste a tester les exports sur des modeles Revit de reference et dans les versions exactes utilisees. Le second graphe utilisateur reste bloque : clusters, valeurs absentes et composants non traduits ne sont pas extrapoles.

## Ressources officielles

- [Code McNeel a la revision utilisee](https://github.com/mcneel/rhino.inside-revit/tree/faa6a97653e9da17c6779103ac39537efff60da8/src/RhinoInside.Revit.GH/Components)
- [Python dans Rhino.Inside.Revit](https://www.rhino3d.com/inside/revit/1.0/guides/rir-ghpython)
- [Autodesk : recherche de parametres et ambiguite des noms](https://help.autodesk.com/cloudhelp/2026/ENU/Revit-API-MainReference/files/html/4400b9f8-3787-0947-5113-2522ff5e5de2.htm)
- [Autodesk : collecteurs et vues valides](https://help.autodesk.com/cloudhelp/2025/ENU/Revit-API/files/Revit_API_Developers_Guide/Basic_Interaction_with_Revit_Elements/Filtering/Revit_API_Revit_API_Developers_Guide_Basic_Interaction_with_Revit_Elements_Filtering_Create_a_FilteredElementCollector_html.html)
- [Autodesk : statuts de Commit](https://help.autodesk.com/cloudhelp/2026/ENU/Revit-API-MainReference/files/html/32714010-7138-f64f-8fde-a310354448e3.htm)

Le catalogue contient le lien direct de chaque implementation C# et les limites du lot. Le script update-node-catalog.ps1 permet de le regenerer depuis les fichiers officiels telecharges et verifies a cette revision.
