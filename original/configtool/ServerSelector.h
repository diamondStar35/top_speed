/**
* Top Speed 3 Config Tool
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
// ServerSelector.h : main header file for the SERVERSELECTOR application
//

#if !defined(AFX_SERVERSELECTOR_H__B100AD96_2B4B_4D29_B7F1_C89B43ADD7D1__INCLUDED_)
#define AFX_SERVERSELECTOR_H__B100AD96_2B4B_4D29_B7F1_C89B43ADD7D1__INCLUDED_

#if _MSC_VER > 1000
#pragma once
#endif // _MSC_VER > 1000

#ifndef __AFXWIN_H__
	#error include 'stdafx.h' before including this file for PCH
#endif

#include "resource.h"		// main symbols

/////////////////////////////////////////////////////////////////////////////
// CServerSelectorApp:
// See ServerSelector.cpp for the implementation of this class
//

class CServerSelectorApp : public CWinApp
{
public:
	CServerSelectorApp();

// Overrides
	// ClassWizard generated virtual function overrides
	//{{AFX_VIRTUAL(CServerSelectorApp)
	public:
	virtual BOOL InitInstance();
	//}}AFX_VIRTUAL

// Implementation

	//{{AFX_MSG(CServerSelectorApp)
		// NOTE - the ClassWizard will add and remove member functions here.
		//    DO NOT EDIT what you see in these blocks of generated code !
	//}}AFX_MSG
	DECLARE_MESSAGE_MAP()
};


/////////////////////////////////////////////////////////////////////////////

//{{AFX_INSERT_LOCATION}}
// Microsoft Visual C++ will insert additional declarations immediately before the previous line.

#endif // !defined(AFX_SERVERSELECTOR_H__B100AD96_2B4B_4D29_B7F1_C89B43ADD7D1__INCLUDED_)
