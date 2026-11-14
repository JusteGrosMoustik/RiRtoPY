# RiRtoPY 0.4.0 : extraire les fonctionnalites pour ChatGPT

Recentrage de l'outil : Grasshopper sert a exprimer visuellement les fonctionnalites a recoder en Python avec ChatGPT. L'interface exporte maintenant un dossier de reecriture plutot que de refuser les graphes non convertibles.

Nouveau bouton Exporter pour ChatGPT :
- .brief.md : prompt de travail, familles fonctionnelles, entrees/sorties, constantes, groupes et libelles, flux entre familles, inconnues a completer.
- .sketch.py : canevas Python de conception, a completer ; aucun resultat executable pretendu.
- .graph.json : annexe detaillee de reference.

Les composants inconnus, clusters, cycles ou arbres ne bloquent plus cette extraction. Aucun contenu interne manquant n'est invente. Les regroupements thematiques ne sont pas presentes comme un ordre d'execution. Les descriptions, noms des groupes directs et textes des Panels non connectes sont desormais captures.

L'ancien catalogue et les moteurs de traduction restent disponibles comme ressources de reecriture, independamment du dossier. Les anciens rapports peuvent deja servir a l'extraction. Aucun document utilisateur n'est publie.

Validation : compilation locale, 69 assertions C# (dont 11 nouvelles sur l'extraction), tests Python existants et verification syntaxique du canevas. Extraction locale du rapport utilisateur de 442 objets reussie sans exiger de .gh ni de traduction des clusters. Nouvelle interface et capture en session Grasshopper a valider manuellement ; aucune execution du graphe ni modification Revit effectuee.

Installer apres sauvegarde et fermeture de Rhino/Revit, puis relancer et reanalyser le graphe pour capturer les nouvelles metadonnees.
