/**
* Top Speed 3
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
// TopSpeed.cpp : Defines the class behaviors for the application.
//

// #define WINVER 0x0501
#include "stdafx.h"
#include "TopSpeed.h"
#include "TopSpeedDlg.h"
#include "Game.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

/////////////////////////////////////////////////////////////////////////////
// CTopSpeedApp

BEGIN_MESSAGE_MAP(CTopSpeedApp, CWinApp)
	//{{AFX_MSG_MAP(CTopSpeedApp)
		// NOTE - the ClassWizard will add and remove mapping macros here.
		//    DO NOT EDIT what you see in these blocks of generated code!
	//}}AFX_MSG
	ON_COMMAND(ID_HELP, CWinApp::OnHelp)
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// CTopSpeedApp construction

CTopSpeedApp::CTopSpeedApp() :
    m_game(0),
    m_initialized(false)
{
    // TODO: add construction code here,
    // Place all significant initialization in InitInstance
}

CTopSpeedApp::~CTopSpeedApp()
{
    m_initialized = false;
    delete m_game;
    m_game = 0;
}

/////////////////////////////////////////////////////////////////////////////
// The one and only CTopSpeedApp object

CTopSpeedApp theApp;

/////////////////////////////////////////////////////////////////////////////
// CTopSpeedApp initialization

BOOL CTopSpeedApp::InitInstance()
{
	// Standard initialization
	// If you are not using these features and wish to reduce the size
	//  of your final executable, you should remove from the following
	//  the specific initialization routines you do not need.

#ifdef _AFXDLL
	// Enable3dControls();			// Call this when using MFC in a shared DLL
#else
	// Enable3dControlsStatic();	// Call this when linking to MFC statically
#endif

    
    m_pDlg = new CTopSpeedDlg();
    if( m_pDlg == NULL )
        return FALSE;

    m_pDlg->Create( IDD_TOPSPEED_DIALOG );

    
    m_pMainWnd = m_pDlg;

CreateMutex(NULL, FALSE, "TSpeed_3");

    File* settings = new File("TopSpeed.cfg", File::read);
    Int enableTracing = 0;
    if (settings->opened( ))
    {
        settings->readInt("EnableTracing", enableTracing, 0);
    }
    else
    {
        SAFE_DELETE(settings);
        settings = new File("TopSpeed.cfg", File::create | File::write);
        fprintf(settings->getStream( ), "[Settings]\n");
        settings->writeKeyInt("EnableTracing", enableTracing);
        settings->writeKeyString("Multiplayer", "127.0.0.1");
    }
    SAFE_DELETE(settings);
    
    File* _file = 0;
    if (enableTracing)
    {
        _file = new File("TopSpeed.trc", File::read | File::write | File::create);
        commonTracer.enable();
        commonTracer.bind(_file);
        _raceTracer.enable();
        _raceTracer.bind(_file);
        dxTracer.enable();
        dxTracer.bind(_file);
    }

    m_game = new Game( );

    m_game->initialize(m_pMainWnd->GetSafeHwnd());    
    
    m_initialized = true;
    m_pDlg->setGame(m_game);
    return TRUE;
    /*
	CTopSpeedDlg dlg;
	m_pMainWnd = &dlg;

    m_game = new Game( );
    m_game->initialize(m_pMainWnd->GetSafeHwnd());

    dlg.setGame(m_game);
    
    m_initialized = true;

    return true;
	int nResponse = dlg.DoModal();
	if (nResponse == IDOK)
	{
		// TODO: Place code here to handle when the dialog is
		//  dismissed with OK
	}
	else if (nResponse == IDCANCEL)
	{
		// TODO: Place code here to handle when the dialog is
		//  dismissed with Cancel
	}

	// Since the dialog has been closed, return FALSE so that we exit the
	//  application, rather than start the application's message pump.
	return FALSE;
    */
}

BOOL CTopSpeedApp::OnIdle(LONG lCount) 
{
	// TODO: Add your specialized code here and/or call the base class
	if (m_initialized)
        m_game->run();
    return true;
	//return CWinApp::OnIdle(lCount);
}
