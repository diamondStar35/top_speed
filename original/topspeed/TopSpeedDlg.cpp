/**
* Top Speed 3
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
// TopSpeedDlg.cpp : implementation file
//

#define WINVER 0x0501
#include "stdafx.h"
#include "TopSpeed.h"
#include "TopSpeedDlg.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

/////////////////////////////////////////////////////////////////////////////
// CTopSpeedDlg dialog

CTopSpeedDlg::CTopSpeedDlg(CWnd* pParent /*=NULL*/) :
    CDialog(CTopSpeedDlg::IDD, pParent),
    m_hIcon(AfxGetApp()->LoadIcon(IDR_MAINFRAME))
{
    //{{AFX_DATA_INIT(CTopSpeedDlg)
    // NOTE: the ClassWizard will add member initialization here
    //}}AFX_DATA_INIT
    // Note that LoadIcon does not require a subsequent DestroyIcon in Win32
}

void CTopSpeedDlg::DoDataExchange(CDataExchange* pDX)
{
    CDialog::DoDataExchange(pDX);
    //{{AFX_DATA_MAP(CTopSpeedDlg)
        // NOTE: the ClassWizard will add DDX and DDV calls here
    //}}AFX_DATA_MAP
}

BEGIN_MESSAGE_MAP(CTopSpeedDlg, CDialog)
    //{{AFX_MSG_MAP(CTopSpeedDlg)
    ON_WM_PAINT()
    ON_WM_QUERYDRAGICON()
    ON_WM_CTLCOLOR()
    //}}AFX_MSG_MAP
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// CTopSpeedDlg message handlers

BOOL CTopSpeedDlg::OnInitDialog()
{
    CDialog::OnInitDialog();

    // Set the icon for this dialog.  The framework does this automatically
    //  when the application's main window is not a dialog
    SetIcon(m_hIcon, TRUE);            // Set big icon
    SetIcon(m_hIcon, FALSE);        // Set small icon
    
    // TODO: Add extra initialization here
    m_bgBrush = CreateSolidBrush(RGB(0,0,0));

    // this->GetDlgItem(IDC_STATIC)->EnableWindow(FALSE);

    return TRUE;  // return TRUE unless you set the focus to a control
}

// If you add a minimize button to your dialog, you will need the code below
//  to draw the icon.  For MFC applications using the document/view model,
//  this is automatically done for you by the framework.

void CTopSpeedDlg::OnPaint() 
{
    if (IsIconic())
    {
        CPaintDC dc(this); // device context for painting

        SendMessage(WM_ICONERASEBKGND, (WPARAM) dc.GetSafeHdc(), 0);

        // Center icon in client rectangle
        int cxIcon = GetSystemMetrics(SM_CXICON);
        int cyIcon = GetSystemMetrics(SM_CYICON);
        CRect rect;
        GetClientRect(&rect);
        int x = (rect.Width() - cxIcon + 1) / 2;
        int y = (rect.Height() - cyIcon + 1) / 2;

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
HCURSOR CTopSpeedDlg::OnQueryDragIcon()
{
    return (HCURSOR) m_hIcon;
}

LRESULT CTopSpeedDlg::WindowProc(UINT message, WPARAM wParam, LPARAM lParam) 
{
    // TODO: Add your specialized code here and/or call the base class
    switch( message )
    {
        // TODO: Repond to Windows messages as needed

        /* case WM_APPCOMMAND:
        case WM_CHAR:
        case WM_DEADCHAR:
        case WM_HOTKEY:
        case WM_KEYDOWN:
        case WM_KEYUP:
        case WM_SYSDEADCHAR:
        case WM_SYSKEYDOWN:
        case WM_SYSKEYUP:
        case WM_UNICHAR:
        case WM_INPUT:
        {
            return TRUE;
        } */
        case WM_COMMAND:
        {
            /* switch( LOWORD(wParam) )
            {
                case IDCANCEL:
                case IDM_EXIT:
                    PostQuitMessage( 0 );
                    break;
                default: */
                    return FALSE; // Message not handled 
            /* }
            break; */
        }
    }
    return CDialog::WindowProc(message, wParam, lParam);
}

HBRUSH CTopSpeedDlg::OnCtlColor(CDC* pDC, CWnd* pWnd, UINT nCtlColor) 
{
    HBRUSH hbr = CDialog::OnCtlColor(pDC, pWnd, nCtlColor);

    switch (nCtlColor) 
    {
        case CTLCOLOR_STATIC:
            // Set the static text to white on blue.
            pDC->SetTextColor(RGB(255, 255, 0));
            pDC->SetBkMode(TRANSPARENT);
            // Drop through to return the background brush.
        case CTLCOLOR_DLG:
            
            return m_bgBrush;
            //return (HBRUSH)(m_pBkBrush.GetSafeHandle());

        default:
            return CDialog::OnCtlColor(pDC, pWnd, nCtlColor);
    }
    
    // TODO: Change any attributes of the DC here
    
    // TODO: Return a different brush if the default is not desired
    return hbr;
}

