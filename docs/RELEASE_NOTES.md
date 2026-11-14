Premiere version de RiRtoPY : analyse de selection Grasshopper, export Python scalaire, rapport de compatibilite JSON, interface et bouton de mise a jour depuis les releases GitHub.

Telecharger l'EXE, enregistrer le travail, fermer Rhino/Revit, puis installer et relancer. Installation par utilisateur sans droits administrateur. Le ZIP permet une installation manuelle.

Prototype limite aux scalaires et aux operations arithmetiques listees dans le README. Les composants Revit, geometries, arbres, listes, scripts et clusters ne sont pas encore traduits. Leur presence bloque l'execution du Python genere.

Compilation et tests automatiques ne remplacent pas la validation dans ta version exacte de Rhino.Inside/Revit. Installateur non signe numeriquement.

Corrections avant publication : chaines Unicode sous IronPython 2, blocage explicite des operations inconnues et des nombres d'entrees invalides, detection des expressions sur parametres autonomes. Validation locale : 32 assertions C# et 6 assertions Python sous IronPython. Installation et utilisation dans Rhino/Revit restent a valider manuellement.
