/**
* Top Speed 3 Config Tool
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
// ServerSelectorDlg.cpp : implementation file
//

#include "stdafx.h"
#include "ServerSelector.h"
#include "ServerSelectorDlg.h"
#include <Common\If\Common.h>


#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

/////////////////////////////////////////////////////////////////////////////
// CServerSelectorDlg dialog

CServerSelectorDlg::CServerSelectorDlg(CWnd* pParent /*=NULL*/)
	: CDialog(CServerSelectorDlg::IDD, pParent)
{
	//{{AFX_DATA_INIT(CServerSelectorDlg)
		// NOTE: the ClassWizard will add member initialization here
	//}}AFX_DATA_INIT
	// Note that LoadIcon does not require a subsequent DestroyIcon in Win32
	m_hIcon = AfxGetApp()->LoadIcon(IDR_MAINFRAME);
}

void CServerSelectorDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
	//{{AFX_DATA_MAP(CServerSelectorDlg)
		// NOTE: the ClassWizard will add DDX and DDV calls here
	//}}AFX_DATA_MAP
}

BEGIN_MESSAGE_MAP(CServerSelectorDlg, CDialog)
	//{{AFX_MSG_MAP(CServerSelectorDlg)
	ON_WM_PAINT()
	ON_WM_QUERYDRAGICON()
	ON_EN_CHANGE(IDC_EDITSERVER, OnChangeEditserver)
	ON_BN_CLICKED(IDCOPY, OnCopy)
	//}}AFX_MSG_MAP
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// CServerSelectorDlg message handlers

BOOL CServerSelectorDlg::OnInitDialog()
{
	CDialog::OnInitDialog();

	// Set the icon for this dialog.  The framework does this automatically
	//  when the application's main window is not a dialog
	SetIcon(m_hIcon, TRUE);			// Set big icon
	SetIcon(m_hIcon, FALSE);		// Set small icon
	
	// TODO: Add extra initialization here
    Int  tracing;
    Char server[MAX_PATH];
    File* internetFile = new File("TopSpeed.cfg", File::read);
    if (!internetFile->opened( ))
    {
        tracing = 0;
        SetDlgItemText(IDC_EDITSERVER, "127.0.0.1");
    }
    else
    {
        if (internetFile->readInt("EnableTracing", tracing, 0))
            CheckDlgButton(IDC_TRACING, tracing);
        else
            CheckDlgButton(IDC_TRACING, 0);
        if (internetFile->readString("Multiplayer", server, MAX_PATH, "127.0.0.1"))
            SetDlgItemText(IDC_EDITSERVER, server);
        else
            SetDlgItemText(IDC_EDITSERVER, "127.0.0.1");
    }
    SAFE_DELETE(internetFile);
    ((CEdit*)this->GetDlgItem(IDC_EDITSERVER))->SetSel(0, -1);
    this->GetDlgItem(IDC_EDITSERVER)->SetFocus( );
    Char yourip[MAX_PATH];
    sprintf(yourip, "Your local IP address is probably %d.%d.%d.%d", 
        (Network::localIP() % 256),
        (Network::localIP() >> 8) % 256,
        (Network::localIP() >> 16) % 256,
        (Network::localIP() >> 24)
        );
    SetDlgItemText(IDC_YOURIP, yourip);
	return FALSE;  // return TRUE unless you set the focus to a control

}

// If you add a minimize button to your dialog, you will need the code below
//  to draw the icon.  For MFC applications using the document/view model,
//  this is automatically done for you by the framework.

void CServerSelectorDlg::OnPaint() 
{
	if (IsIconic())
	{
		CPaintDC dc(this); // device context for painting

		SendMessage(WM_ICONERASEBKGND, (WPARAM) dc.GetSafeHdc(), 0);

		// Center icon in client rectangle
		Int cxIcon = GetSystemMetrics(SM_CXICON);
		Int cyIcon = GetSystemMetrics(SM_CYICON);
		CRect rect;
		GetClientRect(&rect);
		Int x = (rect.Width() - cxIcon + 1) / 2;
		Int y = (rect.Height() - cyIcon + 1) / 2;

		// Draw the icon
		dc.DrawIcon(x, y, m_hIcon);
	}
	else
	{
		CDialog::OnPaint();
	}
}

// The system calls this to obtain the cursor to display while the user drags
//  the minimized window.
HCURSOR CServerSelectorDlg::OnQueryDragIcon()
{
	return (HCURSOR) m_hIcon;
}

void CServerSelectorDlg::OnChangeEditserver() 
{
	// TODO: If this is a RICHEDIT control, the control will not
	// send this notification unless you override the CDialog::OnInitDialog()
	// function and call CRichEditCtrl().SetEventMask()
	// with the ENM_CHANGE flag ORed into the mask.
	
	// TODO: Add your control notification handler code here
	
}

void CServerSelectorDlg::OnOK() 
{
	// TODO: Add extra validation here
    File* internetFile = new File("TopSpeed.cfg", File::write);
    if (!internetFile->opened( ))
    {
        SAFE_DELETE(internetFile);
        internetFile = new File("TopSpeed.cfg", File::create | File::write);
        fprintf(internetFile->getStream( ), "[Settings]\n");
        Int tracing = IsDlgButtonChecked(IDC_TRACING);
        internetFile->writeKeyInt("EnableTracing", tracing);
        Char buffer[MAX_PATH];
        GetDlgItemText(IDC_EDITSERVER, buffer, MAX_PATH);
        internetFile->writeKeyString("Multiplayer", buffer);
    }
    else
    {
        Int tracing = IsDlgButtonChecked(IDC_TRACING);
        internetFile->writeKeyInt("EnableTracing", tracing);
        Char buffer[MAX_PATH];
        GetDlgItemText(IDC_EDITSERVER, buffer, MAX_PATH);
        internetFile->writeKeyString("Multiplayer", buffer);
    }
    SAFE_DELETE(internetFile);
	CDialog::OnOK();
}

void CServerSelectorDlg::OnCopy() 
{
    if (!OpenClipboard()) 
        return; 
    EmptyClipboard(); 
 
    Char buffer[MAX_PATH];
    sprintf(buffer, "%d.%d.%d.%d", 
        (Network::localIP() % 256),
        (Network::localIP() >> 8) % 256,
        (Network::localIP() >> 16) % 256,
        (Network::localIP() >> 24)
        );

    // Allocate a global memory object for the text. 
    HGLOBAL hglbCopy = GlobalAlloc(GMEM_MOVEABLE, 
         ((strlen(buffer) + 1) * sizeof(TCHAR))); 
    if (hglbCopy == NULL) 
    { 
        CloseClipboard(); 
        return; 
    } 
 
    // Lock the handle and copy the text to the buffer. 
    LPTSTR lptstrCopy = (LPTSTR) GlobalLock(hglbCopy); 
    memcpy(lptstrCopy, buffer, 
       strlen(buffer) * sizeof(TCHAR)); 
    lptstrCopy[strlen(buffer)] = (TCHAR) 0;    // null character 
    GlobalUnlock(hglbCopy); 
 
    // Place the handle on the clipboard. 
    SetClipboardData(CF_TEXT, hglbCopy); 
    CloseClipboard(); 
}
