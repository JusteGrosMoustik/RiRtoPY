# RiRtoPY 0.2.0

Ajout experimental de la chaine exacte Query Rooms -> Surface -> Area -> Mass Addition -> Panel terminal. Cible : RhinoInside.Revit.GH 1.35. Filtres Point, Number, Name et Level absents ; Placed=True ; derniere phase du document. Les variantes restent bloquees.

Export Python reutilisable en lecture seule dans Rhino.Inside.Revit : surfaces rognees des pieces via RoomElement.TrimmedSurface, aires et centroides Rhino, somme et sommes cumulees. Unites : unites du modele Rhino au carre. Le Panel terminal est expose comme valeur numerique ; son formatage visuel n'est pas reproduit.

Rapport schema 2 : distinction entre entree optionnelle vide et donnee persistante non scalaire. Reanalyser le graphe avec 0.2.0 ; les rapports 0.1.0 ne suffisent pas pour activer ce profil.

Tests automatiques locaux : 32 assertions C# scalaires, 14 assertions C# du profil, 6 assertions Python scalaires et scenarios Python de controle avec doublures API. Aucun calcul geometrique reel dans Revit n'a ete valide. L'interface 0.1.0 a ete observee chargee dans Rhino 9 / Rhino.Inside.Revit sur la capture utilisateur ; cette observation ne valide pas le nouveau profil.

Installation par utilisateur, sans droits administrateur. Enregistrer le travail et fermer Rhino/Revit avant l'installation ; relancer ensuite. Installateur non signe numeriquement.
