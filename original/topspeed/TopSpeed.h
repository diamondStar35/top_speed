/**
* Top Speed 3
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
// TopSpeed.h : main header file for the TOPSPEED application
//

#if !defined(AFX_TOPSPEED_H__DD3408B7_CB96_4405_94AE_FD1DF30F1EFD__INCLUDED_)
#define AFX_TOPSPEED_H__DD3408B7_CB96_4405_94AE_FD1DF30F1EFD__INCLUDED_

#if _MSC_VER > 1000
#pragma once
#endif // _MSC_VER > 1000

#ifndef __AFXWIN_H__
	#error include 'stdafx.h' before including this file for PCH
#endif

#include "resource.h"		// main symbols

/////////////////////////////////////////////////////////////////////////////
// CTopSpeedApp:
// See TopSpeed.cpp for the implementation of this class
//

class CTopSpeedDlg;
class Game;

class CTopSpeedApp : public CWinApp
{
public:
	CTopSpeedApp();
    ~CTopSpeedApp();

// Overrides
	// ClassWizard generated virtual function overrides
	//{{AFX_VIRTUAL(CTopSpeedApp)
	public:
	virtual BOOL InitInstance();
	virtual BOOL OnIdle(LONG lCount);
	//}}AFX_VIRTUAL

// Implementation

	//{{AFX_MSG(CTopSpeedApp)
		// NOTE - the ClassWizard will add and remove member functions here.
		//    DO NOT EDIT what you see in these blocks of generated code !
	//}}AFX_MSG
	DECLARE_MESSAGE_MAP()

private:
    CTopSpeedDlg*   m_pDlg;
    Game*           m_game;
    bool            m_initialized;

};


/////////////////////////////////////////////////////////////////////////////

//{{AFX_INSERT_LOCATION}}
// Microsoft Visual C++ will insert additional declarations immediately before the previous line.

#endif // !defined(AFX_TOPSPEED_H__DD3408B7_CB96_4405_94AE_FD1DF30F1EFD__INCLUDED_)
