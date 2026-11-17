!macro customInstall
  CreateShortCut "$DESKTOP\Dofus Galaxy Tabs.lnk" "$INSTDIR\resources\DofusGalaxyTabs.exe" "" "$INSTDIR\resources\DofusGalaxyTabs.exe" 0
  CreateShortCut "$SMPROGRAMS\Dofus Galaxy Tabs.lnk" "$INSTDIR\resources\DofusGalaxyTabs.exe" "" "$INSTDIR\resources\DofusGalaxyTabs.exe" 0
!macroend

!macro customUnInstall
  Delete "$DESKTOP\Dofus Galaxy Tabs.lnk"
  Delete "$SMPROGRAMS\Dofus Galaxy Tabs.lnk"
!macroend
