/**
* Top Speed 3
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
// TopSpeedDlg.h : header file
//

#if !defined(AFX_TOPSPEEDDLG_H__E135110A_5634_454D_B21F_29F7B2AAAE16__INCLUDED_)
#define AFX_TOPSPEEDDLG_H__E135110A_5634_454D_B21F_29F7B2AAAE16__INCLUDED_

#if _MSC_VER > 1000
#pragma once
#endif // _MSC_VER > 1000

/////////////////////////////////////////////////////////////////////////////
// CTopSpeedDlg dialog

class Game;

class CTopSpeedDlg : public CDialog
{
// Construction
public:
	CTopSpeedDlg(CWnd* pParent = NULL);	// standard constructor

// Dialog Data
	//{{AFX_DATA(CTopSpeedDlg)
	enum { IDD = IDD_TOPSPEED_DIALOG };
		// NOTE: the ClassWizard will add data members here
	//}}AFX_DATA

	// ClassWizard generated virtual function overrides
	//{{AFX_VIRTUAL(CTopSpeedDlg)
	protected:
	virtual void DoDataExchange(CDataExchange* pDX);	// DDX/DDV support
	virtual LRESULT WindowProc(UINT message, WPARAM wParam, LPARAM lParam);
	//}}AFX_VIRTUAL

// Implementation
public:
    void setGame(Game* game)        { m_game = game; };

protected:
	HICON  m_hIcon;
    Game*  m_game;
    HBRUSH m_bgBrush;


	// Generated message map functions
	//{{AFX_MSG(CTopSpeedDlg)
	virtual BOOL OnInitDialog();
	afx_msg void OnPaint();
	afx_msg HCURSOR OnQueryDragIcon();
	afx_msg HBRUSH OnCtlColor(CDC* pDC, CWnd* pWnd, UINT nCtlColor);
	//}}AFX_MSG
	DECLARE_MESSAGE_MAP()
};

//{{AFX_INSERT_LOCATION}}
// Microsoft Visual C++ will insert additional declarations immediately before the previous line.

#endif // !defined(AFX_TOPSPEEDDLG_H__E135110A_5634_454D_B21F_29F7B2AAAE16__INCLUDED_)
