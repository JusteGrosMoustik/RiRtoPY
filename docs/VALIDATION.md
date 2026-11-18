# Validation 0.6.0

## Objectif

Verifier que le dossier fourni a ChatGPT/Codex permet une conversion Grasshopper vers Python plus fidele : code exact quand disponible, interfaces et dependances traceables, donnees et unites explicites, aucune formule inventee ni preuve de calcul attribuee a un simple instantane.

## Controles effectues localement

- Compilation plugin/installateur contre les DLL Rhino 7 installees.
- 22 assertions existantes sur le rendu exhaustif et la serialisation (arbres, valeurs nulles, sources ordonnees, clusters imbriques, gros volumes et longs textes).
- 48 assertions supplementaires : adaptateurs VB/C#/GhPython/RhinoCode, sections de code et references, erreurs precises et sources externes, dependances par sortie et clusters imbriques, cycles, perimetre projete, diagnostics globaux et categories, indices invalides, masquage dans le JSON reel, exclusion des valeurs/sources sans modifier la capture originale, comparaison des etats, rapports compacts, contrats de conversion et extraits explicitement limites.
- 4 assertions geometriques executees avec le SDK reel sur point, vecteur et ligne simples, et rejet du texte non geometrique. Elles ne valident pas les mesures natives de courbes/surfaces/Breps.
- Controle du paquet : plugin, README et VERSION seulement ; aucun fichier utilisateur ni ancien moteur/bibliotheque Python dans la distribution.
- Les programmes de tests interceptent leurs erreurs et retournent un code d'echec sans fenetre Windows.

## Fichier de reference Rampe1.61.gh

Une copie locale ignoree de Git a ete lue avec GH_IO, sans ouvrir GH_Document, sans resoudre les composants, sans lancer Rhino ou Grasshopper et sans calcul. L'original du partage n'a pas ete modifie. Les sources du document ne sont pas publiees dans le depot.

`scripts/test-reference.ps1 -ReferenceFile 'chemin\Rampe1.61.gh' -RampeBaseline` verifie cette baseline :

| Observation dans l'archive | Resultat |
|---|---:|
| Objets, groupes compris | 1072 |
| Groupes tous documents accessibles | 134 |
| Objets hors groupes | 938 |
| Scribbles avec texte | 157 |
| Scripts avec source preservee a l'identique par l'adaptateur archive | 14 |
| Sections de source | 15 |
| Clusters rencontres | 15 |
| Documents internes ouverts | 11 |
| Clusters proteges | 4 |
| References Source serialisees | 927 |
| References Source resolues dans leur document | 926 |
| Reference Source archive absente | 1 |

La reference orpheline est associee a un parametre texte. C'est une anomalie du fichier serialise ; elle n'est pas assimilee aux connexions effectivement reconstruites par Grasshopper. L'audit utilisateur annoncait 926 connexions valides sur l'export 0.5.0. Aucune nouvelle session live ne permet actuellement de confirmer comment Grasshopper nettoie cette reference a l'ouverture.

Ces verifications prouvent la presence et la recuperabilite des sources et annotations dans l'archive. Elles ne prouvent pas a elles seules la capture par les proprietes runtime des composants, ni le calcul du fichier.

## Essais Grasshopper restant a effectuer

Aucune session de controle Grasshopper connectee n'etait disponible pendant cette revision. La capture complete 0.6.0, l'interface et les fonctions geometriques natives restent a tester dans l'hote reel.

1. Installer apres sauvegarde/fermeture, ouvrir la copie de reference et garder le Solver desactive pendant l'extraction.
2. Verifier les 938 objets, 926 connexions live attendues, 15 clusters, 11 interieurs, quatre limites de protection et 14 statuts de source explicites. Comparer code principal, imports et code additionnel avec les editeurs des scripts, notamment dans les clusters.
3. Verifier les Scribbles, les noms bruts vides des groupes, leurs couleurs et leurs limites ; deplacer un objet et verifier la capture suivante.
4. Sur une copie de test avec deux sorties independantes et un sous-cluster, cibler une sortie : ses dependances doivent etre incluses, la branche independante doit etre exclue. La fonction interne d'un composant reste traitee de facon conservative.
5. Mesurer une courbe, une surface et un Brep connus : longueur, rayon, aire, domaine, normale echantillonnee, faces, volume, bbox et unites. Verifier qu'une reference non chargee reste indisponible.
6. Capturer un composant avec erreur et un avec avertissement : categorie runtime, sans confondre avec un echec d'extraction. Verifier la remontee des diagnostics de clusters.
7. Modifier successivement un slider, un fil, un script et une position ; comparer les JSON et verifier les categories. Avec deux etats identiques, aucun changement de logique ne doit etre annonce.
8. Exporter avec les trois niveaux et filtres ; verifier les trois fichiers, les extraits marques, les chemins masques y compris dans le code, l'exclusion des valeurs et l'exclusion des sources. Les noms/annotations restent visibles et doivent etre relus avant partage.
9. Observer une fin de solution uniquement dans une copie de test autorisee : la capture suivante peut alors indiquer cette date observee. Une date anterieure au premier contact doit rester inconnue. Cela ne constitue toujours pas une validation du calcul.
10. Utiliser le dossier pour transcrire un premier module Python ; comparer ses entrees/sorties, branches et unites avec une execution Grasshopper de reference autorisee. Cette derniere etape n'a pas ete effectuee ici.

Les modules fonctionnels sont des hypotheses structurelles, pas une interpretation certifiee du metier. La compatibilite des adaptateurs entre versions Rhino doit etre verifiee a l'execution. Les anciens tests de traduction restent dans scripts/test-legacy.ps1 et ne prouvent pas la conversion automatique de cette definition.
