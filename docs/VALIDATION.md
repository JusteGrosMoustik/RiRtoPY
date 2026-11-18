# Validation 0.5.0

## Verifications locales

- Compilation du plugin et de l'installateur contre les DLL Rhino 7 installees.
- 22 assertions automatiques sur le rapport : fichier, groupes, clusters a plusieurs niveaux, instances partageant des GUID internes, connexions, indices de ports, sources multiples ordonnees, passage de cluster, texte de Panel multiligne, valeurs de Panel connecte, chemins de branches, nulls, donnees persistantes/volatiles, modificateurs, expressions, extraction incomplete, erreur de valeur, source absente, long texte sans troncature, JSON indente, depassement du quota habituel de serialisation, aller-retour JSON et document vide.
- Le test utilise des modeles construits en memoire. Il valide le rendu et la serialisation, pas la capture reelle dans Grasshopper.
- La structure ParamMap a ete examinee dans le SDK Rhino 7 installe : Key = GUID du parametre externe, Value = GUID du point de passage interne. L'export signale toute absence ou incoherence plutot que de deviner l'ordre des points de passage.
- Le test automatique intercepte ses erreurs et retourne un code d'echec sans ouvrir de boite de dialogue Windows.

Une tentative de construction de GH_Document hors de Rhino a echoue faute d'initialisation du moteur natif Rhino. Elle n'est pas une validation du lecteur en execution. Aucun graphe utilisateur ni document Revit n'a ete execute ou modifie.

## Essai dans Grasshopper a effectuer

1. Installer la nouvelle version apres sauvegarde et fermeture de Rhino/Revit.
2. Preparer un fichier de test avec deux groupes imbriques, un Panel multiline non connecte, un Panel connecte, un slider, un toggle, une liste de valeurs et un parametre contenant plusieurs branches et valeurs nulles.
3. Inclure un cluster contenant un sous-cluster et plusieurs entrees/sorties. Donner des noms identiques aux points de passage et changer leur position, pour verifier que les correspondances suivent les GUID plutot que les noms ou la position.
4. Dupliquer un cluster. Verifier que leurs identifiants scopes et leurs connexions restent distincts.
5. Analyser et exporter. Comparer chaque objet, groupe, branche et connexion des deux documents internes avec le .md et le .graph.json.
6. Modifier un Panel apres l'analyse puis exporter : le contenu exporte doit etre actualise.
7. Tester un cluster protege : extraction incomplete explicite, autres objets toujours exportes. Ne pas contourner la protection.
8. Desactiver le Solver, vider les donnees d'un composant et exporter : les donnees manquantes doivent etre signalees, aucun recalcul ne doit se produire.
9. Verifier le chargement sous les versions Rhino/Grasshopper effectivement utilisees, notamment ParamMap. La compatibilite du schema de serialisation entre versions n'est pas supposee acquise.

Les anciens tests de traduction sont conserves dans scripts/test-legacy.ps1. Ils ne valident pas l'extracteur 0.5.0 et ne font plus partie du parcours principal.
