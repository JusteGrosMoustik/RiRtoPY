# RiRtoPY 0.5.0 : releve complet pour analyse avec ChatGPT

Objectif unique : transmettre le fichier Grasshopper a ChatGPT sous une forme lisible et structuree.

- Export du document complet, sans filtre de selection.
- Inventaire des composants et de leurs groupes, y compris groupes englobants.
- Descente recursive dans les clusters accessibles et leurs clusters imbriques.
- Identifiants scopes par instance de cluster pour eviter les collisions.
- Connexions port par port et correspondances externes/internes lues dans ParamMap.
- Panels connectes et non connectes, sliders, toggles, listes de valeurs.
- Donnees persistantes et volatiles, branches, types, nulls, expressions et modificateurs.
- Markdown exhaustif et JSON indente ; plus de canevas ni de bibliotheque Python dans la distribution.
- Document relu a l'export pour eviter une capture perimee dans l'apercu.
- Erreurs, clusters proteges et references non resolues signales comme extraction incomplete.

Compilation locale plugin/installateur et 22 assertions automatiques reussies sur les rapports et leur serialisation. La capture dans une vraie session Grasshopper et les correspondances des ports sur un fichier reel restent a valider. Aucun calcul Grasshopper ni aucune modification Revit n'a ete effectue.

Installer apres sauvegarde et fermeture de Rhino/Revit, puis relancer les logiciels.
