# RiRtoPY 0.3.0 : noeuds Revit generaux

Premier lot de traductions base sur les sources officielles McNeel RiR 1.35 : catalogue de 381 composants dans 317 fichiers, avec type CLR, GUID, source et statut individuel.

12 composants avec generation Python automatique restreinte : Active Document, Query Element(s), Element Category, Element Name en lecture, Pin Element en lecture, Element View, Category Filter, Exclude Element Types Filter, Parameter Filter, Intersection Filter, Union Filter. Support d'une liste plate pour les accesseurs d'elements, sources natives explicites via le dictionnaire inputs, sorties nommees et indices de ports valides.

15 autres composants disposent de helpers manuels partiels (requetes, types, identite, materiaux, parametres et filtres logiques). Les fonctions renvoient des objets DB natifs ; les differences de filtres, unites et types sont documentees dans GENERAL_NODES.md. Les primitives manuelles d'ecriture exigent une transaction ouverte par l'appelant. La conversion automatique reste en lecture seule.

Le profil pieces 0.2.0 et le profil scalaire sont conserves. Arbres generaux, clusters, scripts, reconstruction/suivi des elements et le grand graphe utilisateur complet restent non traduits. Aucun contenu utilisateur fourni n'est publie.

Nouveau telechargement RiRtoPY-Python-0.3.0.zip : bibliotheque rir_nodes.py, catalogue JSON et guide. La bibliotheque est aussi incorporee au plugin pour generer des scripts autonomes.

Validation locale : compilation du plugin et de l'installateur ; 58 assertions C# (32 scalaires + 14 profil pieces + 12 generales), 6 assertions Python scalaires, scenarios pieces et 49 assertions Python generales sous IronPython 2.7. Les tests des appels Revit utilisent des doublures API. Aucun nouveau profil n'a ete valide dans Revit ; verifier sur un modele de test. Le workflow execute aussi les tests sous Python 3.

Installer apres sauvegarde et fermeture de Rhino/Revit, puis relancer et reanalyser le graphe pour produire un rapport schema 2. Installation par utilisateur, sans droits administrateur, installateur non signe numeriquement.
