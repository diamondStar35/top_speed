; CLW file contains information for the MFC ClassWizard

[General Info]
Version=1
LastClass=CServerSelectorDlg
LastTemplate=CDialog
NewFileInclude1=#include "stdafx.h"
NewFileInclude2=#include "ServerSelector.h"

ClassCount=2
Class1=CServerSelectorApp
Class2=CServerSelectorDlg

ResourceCount=4
Resource2=IDR_MAINFRAME
Resource3=IDD_SERVERSELECTOR_DIALOG
Resource4=IDD_SERVERSELECTOR_DIALOG (English (U.S.))

[CLS:CServerSelectorApp]
Type=0
HeaderFile=ServerSelector.h
ImplementationFile=ServerSelector.cpp
Filter=N

[CLS:CServerSelectorDlg]
Type=0
HeaderFile=ServerSelectorDlg.h
ImplementationFile=ServerSelectorDlg.cpp
Filter=D
BaseClass=CDialog
VirtualFilter=dWC
LastObject=IDC_EDITSERVER



[DLG:IDD_SERVERSELECTOR_DIALOG]
Type=1
ControlCount=3
Control1=IDOK,button,1342242817
Control2=IDCANCEL,button,1342242816
Control3=IDC_STATIC,static,1342308352
Class=CServerSelectorDlg

[DLG:IDD_SERVERSELECTOR_DIALOG (English (U.S.))]
Type=1
Class=CServerSelectorDlg
ControlCount=6
Control1=IDOK,button,1342242817
Control2=IDCANCEL,button,1342242816
Control3=IDC_EDITSERVER,edit,1350631552
Control4=IDC_STATIC,static,1342308352
Control5=IDC_YOURIP,static,1342308352
Control6=IDCOPY,button,1342242817

