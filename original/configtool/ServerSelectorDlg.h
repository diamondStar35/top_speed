/**
* Top Speed 3 Config Tool
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
// ServerSelectorDlg.h : header file
//

#if !defined(AFX_SERVERSELECTORDLG_H__1502588C_3E1A_43CF_A33B_D66C78D6841E__INCLUDED_)
#define AFX_SERVERSELECTORDLG_H__1502588C_3E1A_43CF_A33B_D66C78D6841E__INCLUDED_

#if _MSC_VER > 1000
#pragma once
#endif // _MSC_VER > 1000

/////////////////////////////////////////////////////////////////////////////
// CServerSelectorDlg dialog

class CServerSelectorDlg : public CDialog
{
// Construction
public:
	CServerSelectorDlg(CWnd* pParent = NULL);	// standard constructor

// Dialog Data
	//{{AFX_DATA(CServerSelectorDlg)
	enum { IDD = IDD_SERVERSELECTOR_DIALOG };
		// NOTE: the ClassWizard will add data members here
	//}}AFX_DATA

	// ClassWizard generated virtual function overrides
	//{{AFX_VIRTUAL(CServerSelectorDlg)
	protected:
	virtual void DoDataExchange(CDataExchange* pDX);	// DDX/DDV support
	//}}AFX_VIRTUAL

// Implementation
protected:
	HICON m_hIcon;

	// Generated message map functions
	//{{AFX_MSG(CServerSelectorDlg)
	virtual BOOL OnInitDialog();
	afx_msg void OnPaint();
	afx_msg HCURSOR OnQueryDragIcon();
	afx_msg void OnChangeEditserver();
	virtual void OnOK();
	afx_msg void OnCopy();
	//}}AFX_MSG
	DECLARE_MESSAGE_MAP()
};

//{{AFX_INSERT_LOCATION}}
// Microsoft Visual C++ will insert additional declarations immediately before the previous line.

#endif // !defined(AFX_SERVERSELECTORDLG_H__1502588C_3E1A_43CF_A33B_D66C78D6841E__INCLUDED_)
