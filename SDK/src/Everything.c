
//
// Copyright (C) 2016 David Carpenter
// 
// Permission is hereby granted, free of charge, 
// to any person obtaining a copy of this software 
// and associated documentation files (the "Software"), 
// to deal in the Software without restriction, 
// including without limitation the rights to use, 
// copy, modify, merge, publish, distribute, sublicense, 
// and/or sell copies of the Software, and to permit 
// persons to whom the Software is furnished to do so, 
// subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be 
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, 
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES 
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, 
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, 
// TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

// Notes:
// this project builds the dll (visual studio will also build the lib for us)
// we declare all exported calls to __stdcall, so theres no need to set the default calling standard.

// disable warnings
#pragma warning(disable : 4996) // deprecation

#define EVERYTHINGUSERAPI __declspec(dllexport)

// include
#include "../include/Everything.h"
#include "../ipc/Everything_IPC.h"

// return copydata code
#define _EVERYTHING_COPYDATA_QUERYREPLY		0

#define _EVERYTHING_MSGFLT_ALLOW		1

typedef struct _EVERYTHING_tagCHANGEFILTERSTRUCT 
{
	DWORD cbSize;
	DWORD ExtStatus;
}_EVERYTHING_CHANGEFILTERSTRUCT, *_EVERYTHING_PCHANGEFILTERSTRUCT;

static void *_Everything_Alloc(DWORD size);
static void _Everything_Free(void *ptr);
static void _Everything_Initialize(void);
static void _Everything_Lock(void);
static void _Everything_Unlock(void);
static DWORD _Everything_StringLengthA(LPCSTR start);
static DWORD _Everything_StringLengthW(LPCWSTR start);
static BOOL EVERYTHINGAPI _Everything_Query(void);
static BOOL _Everything_ShouldUseVersion2(void);
static BOOL _Everything_SendIPCQuery(void);
static BOOL _Everything_SendIPCQuery2(HWND everything_hwnd);
static void _Everything_FreeLists(void);
static BOOL _Everything_IsValidResultIndex(DWORD dwIndex);
static void *_Everything_GetRequestData(DWORD dwIndex,DWORD dwRequestType);
static BOOL _Everything_IsSchemeNameW(LPCWSTR s);
static BOOL _Everything_IsSchemeNameA(LPCSTR s);
static void _Everything_ChangeWindowMessageFilter(HWND hwnd);
static BOOL _Everything_GetResultRequestData(DWORD dwIndex,DWORD dwRequestType,void *data,int size);
static LPCWSTR _Everything_GetResultRequestStringW(DWORD dwIndex,DWORD dwRequestType);
static LPCSTR _Everything_GetResultRequestStringA(DWORD dwIndex,DWORD dwRequestType);
static BOOL _Everything_SendAPIBoolCommand(int command,LPARAM lParam);
static DWORD _Everything_SendAPIDwordCommand(int command,LPARAM lParam);
static LRESULT _Everything_SendCopyData(int command,const void *data,int size);
static LRESULT WINAPI _Everything_window_proc(HWND hwnd,UINT msg,WPARAM wParam,LPARAM lParam);

// internal state
static BOOL _Everything_MatchPath = FALSE;
static BOOL _Everything_MatchCase = FALSE;
static BOOL _Everything_MatchWholeWord = FALSE;
static BOOL _Everything_Regex = FALSE;
static DWORD _Everything_LastError = FALSE;
static DWORD _Everything_Max = EVERYTHING_IPC_ALLRESULTS;
static DWORD _Everything_Offset = 0;
static DWORD _Everything_Sort = EVERYTHING_SORT_NAME_ASCENDING;
static DWORD _Everything_RequestFlags = EVERYTHING_REQUEST_PATH | EVERYTHING_REQUEST_FILE_NAME;
static BOOL _Everything_IsUnicodeQuery = FALSE;
static DWORD _Everything_QueryVersion = 0;
static BOOL _Everything_IsUnicodeSearch = FALSE;
static void *_Everything_Search = NULL; // wchar or char
static EVERYTHING_IPC_LIST2 *_Everything_List2 = NULL;
static void *_Everything_List = NULL; // EVERYTHING_IPC_LISTW or EVERYTHING_IPC_LISTA
static volatile BOOL _Everything_Initialized = FALSE;
static volatile LONG _Everything_InterlockedCount = 0;
static CRITICAL_SECTION _Everything_cs;
static HWND _Everything_ReplyWindow = 0;
static DWORD _Everything_ReplyID = 0;
static BOOL (WINAPI *_Everything_pChangeWindowMessageFilterEx)(HWND hWnd,UINT message,DWORD action,_EVERYTHING_PCHANGEFILTERSTRUCT pChangeFilterStruct) = 0;
static HANDLE _Everything_user32_hdll = NULL;
static BOOL _Everything_GotChangeWindowMessageFilterEx = FALSE;

static void _Everything_Initialize(void)
{
	if (!_Everything_Initialized)
	{	
		if (InterlockedIncrement(&_Everything_InterlockedCount) == 1)
		{
			// do the initialization..
			InitializeCriticalSection(&_Everything_cs);
			
			_Everything_Initialized = 1;
		}
		else
		{
			// wait for initialization by other thread.
			while (!_Everything_Initialized) Sleep(0);
		}
	}
}

static void _Everything_Lock(void)
{
	_Everything_Initialize();
	
	EnterCriticalSection(&_Everything_cs);
}

static void _Everything_Unlock(void)
{
	LeaveCriticalSection(&_Everything_cs);
}

// avoid other libs
static DWORD _Everything_StringLengthA(LPCSTR start)
{
	register LPCSTR s;
	
	s = start;
	
	while(*s)
	{
		s++;
	}
	
	return (DWORD)(s-start);
}

static DWORD _Everything_StringLengthW(LPCWSTR start)
{
	register LPCWSTR s;
	
	s = start;
	
	while(*s)
	{
		s++;
	}
	
	return (DWORD)(s-start);
}

void EVERYTHINGAPI Everything_SetSearchW(LPCWSTR lpString)
{
	DWORD len;
	
	_Everything_Lock();
	
	if (_Everything_Search) 
	{
		_Everything_Free(_Everything_Search);
	}
	
	len = _Everything_StringLengthW(lpString) + 1;

	_Everything_Search = _Everything_Alloc(len*sizeof(WCHAR));
	if (_Everything_Search)
	{
		CopyMemory(_Everything_Search,lpString,len*sizeof(WCHAR));
	}
	else
	{
		_Everything_LastError = EVERYTHING_ERROR_MEMORY;
	}
	
	_Everything_IsUnicodeSearch = 1;
	
	_Everything_Unlock();
}

void EVERYTHINGAPI Everything_SetSearchA(LPCSTR lpString)
{
	DWORD size;
	
	_Everything_Lock();
	
	if (_Everything_Search) 
	{
		_Everything_Free(_Everything_Search);
	}
	
	size = _Everything_StringLengthA(lpString) + 1;

	_Everything_Search = _Everything_Alloc(size);
	if (_Everything_Search)
	{
		CopyMemory(_Everything_Search,lpString,size);
	}
	else
	{
		_Everything_LastError = EVERYTHING_ERROR_MEMORY;
	}

	_Everything_IsUnicodeSearch = 0;

	_Everything_Unlock();
}

LPCSTR EVERYTHINGAPI Everything_GetSearchA(void)
{
	LPCSTR ret;
	
	_Everything_Lock();
	
	if (_Everything_Search)
	{
		if (_Everything_IsUnicodeSearch)
		{
			_Everything_LastError = EVERYTHING_ERROR_INVALIDCALL;
			
			ret = NULL;
		}
		else
		{
			ret = (LPCSTR)_Everything_Search;
		}
	}
	else
	{
		ret = "";
	}

	_Everything_Unlock();

	return ret;
}

LPCWSTR EVERYTHINGAPI Everything_GetSearchW(void)
{
	LPCWSTR ret;
	
	_Everything_Lock();

	if (_Everything_Search)
	{
		if (!_Everything_IsUnicodeSearch)
		{
			_Everything_LastError = EVERYTHING_ERROR_INVALIDCALL;
			
			ret = NULL;
		}
		else
		{
			ret = (LPCWSTR)_Everything_Search;
		}
	}
	else
	{
		ret = L"";
	}
	
	_Everything_Unlock();

	return ret;
}

void EVERYTHINGAPI Everything_SetMatchPath(BOOL bEnable)
{
	_Everything_Lock();

	_Everything_MatchPath = bEnable;

	_Everything_Unlock();
}

void EVERYTHINGAPI Everything_SetMatchCase(BOOL bEnable)
{
	_Everything_Lock();

	_Everything_MatchCase = bEnable;

	_Everything_Unlock();
}

void EVERYTHINGAPI Everything_SetMatchWholeWord(BOOL bEnable)
{
	_Everything_Lock();

	_Everything_MatchWholeWord = bEnable;

	_Everything_Unlock();
}

void EVERYTHINGAPI Everything_SetRegex(BOOL bEnable)
{
	_Everything_Lock();

	_Everything_Regex = bEnable;

	_Everything_Unlock();
}

void EVERYTHINGAPI Everything_SetMax(DWORD dwMax)
{
	_Everything_Lock();

	_Everything_Max = dwMax;

	_Everything_Unlock();
}

void EVERYTHINGAPI Everything_SetOffset(DWORD dwOffset)
{
	_Everything_Lock();

	_Everything_Offset = dwOffset;

	_Everything_Unlock();
}

void EVERYTHINGAPI Everything_SetSort(DWORD dwSort)
{
	_Everything_Lock();

	_Everything_Sort = dwSort;

	_Everything_Unlock();
}

EVERYTHINGUSERAPI void EVERYTHINGAPI Everything_SetRequestFlags(DWORD dwRequestFlags)
{
	_Everything_Lock();

	_Everything_RequestFlags = dwRequestFlags;

	_Everything_Unlock();
}

void EVERYTHINGAPI Everything_SetReplyWindow(HWND hWnd)
{
	_Everything_Lock();

	_Everything_ReplyWindow = hWnd;

	_Everything_Unlock();
}
	
void EVERYTHINGAPI Everything_SetReplyID(DWORD dwId)
{
	_Everything_Lock();

	_Everything_ReplyID = dwId;

	_Everything_Unlock();
}
	
BOOL EVERYTHINGAPI Everything_GetMatchPath(void)
{
	BOOL ret;
	
	_Everything_Lock();
	
	ret = _Everything_MatchPath;

	_Everything_Unlock();
	
	return ret;
}

BOOL EVERYTHINGAPI Everything_GetMatchCase(void)
{
	BOOL ret;
	
	_Everything_Lock();
	
	ret = _Everything_MatchCase;

	_Everything_Unlock();
	
	return ret;
}

BOOL EVERYTHINGAPI Everything_GetMatchWholeWord(void)
{
	BOOL ret;
	
	_Everything_Lock();
	
	ret = _Everything_MatchWholeWord;

	_Everything_Unlock();
	
	return ret;
}

BOOL EVERYTHINGAPI Everything_GetRegex(void)
{
	BOOL ret;
	
	_Everything_Lock();
	
	ret = _Everything_Regex;

	_Everything_Unlock();
	
	return ret;
}

DWORD EVERYTHINGAPI Everything_GetMax(void)
{
	DWORD ret;
	
	_Everything_Lock();
	
	ret = _Everything_Max;

	_Everything_Unlock();
	
	return ret;
}

DWORD EVERYTHINGAPI Everything_GetOffset(void)
{
	DWORD ret;
	
	_Everything_Lock();
	
	ret = _Everything_Offset;

	_Everything_Unlock();
	
	return ret;
}

DWORD EVERYTHINGAPI Everything_GetSort(void)
{
	DWORD ret;
	
	_Everything_Lock();
	
	ret = _Everything_Sort;

	_Everything_Unlock();
	
	return ret;
}

EVERYTHINGUSERAPI DWORD EVERYTHINGAPI Everything_GetRequestFlags(void)
{
	DWORD ret;
	
	_Everything_Lock();
	
	ret = _Everything_RequestFlags;

	_Everything_Unlock();
	
	return ret;
}

HWND EVERYTHINGAPI Everything_GetReplyWindow(void)
{
	HWND ret;
	
	_Everything_Lock();

	ret = _Everything_ReplyWindow;

	_Everything_Unlock();
	
	return ret;
}
	
DWORD EVERYTHINGAPI Everything_GetReplyID(void)
{
	DWORD ret;
	
	_Everything_Lock();

	ret = _Everything_ReplyID;

	_Everything_Unlock();
	
	return ret;
}
	
// custom window proc
static LRESULT WINAPI _Everything_window_proc(HWND hwnd,UINT msg,WPARAM wParam,LPARAM lParam)
{
	switch(msg)
	{
		case WM_COPYDATA:
		{
			COPYDATASTRUCT *cds = (COPYDATASTRUCT *)lParam;
			
			switch(cds->dwData)
			{
				case _EVERYTHING_COPYDATA_QUERYREPLY:
					
					if (_Everything_QueryVersion == 2)
					{
						_Everything_FreeLists();
						
						_Everything_List2 = _Everything_Alloc(cds->cbData);
						
						if (_Everything_List2)
						{
							CopyMemory(_Everything_List2,cds->lpData,cds->cbData);
						}
						else
						{
							_Everything_LastError = EVERYTHING_ERROR_MEMORY;
						}
						
						PostQuitMessage(0);
					}
					else
					if (_Everything_QueryVersion == 1)
					{
						_Everything_FreeLists();
						
						_Everything_List = _Everything_Alloc(cds->cbData);
						
						if (_Everything_List)
						{
							CopyMemory(_Everything_List,cds->lpData,cds->cbData);
						}
						else
						{
							_Everything_LastError = EVERYTHING_ERROR_MEMORY;
						}
						
						PostQuitMessage(0);

						return TRUE;
					}
					
					break;
			}
			
			break;
		}
	}
	
	return DefWindowProc(hwnd,msg,wParam,lParam);
}

// get the search length
static DWORD _Everything_GetSearchLengthW(void)
{
	if (_Everything_Search)
	{
		if (_Everything_IsUnicodeSearch)
		{
			return _Everything_StringLengthW((LPCWSTR )_Everything_Search);
		}
		else
		{
			return MultiByteToWideChar(CP_ACP,0,(LPCSTR )_Everything_Search,-1,0,0);
		}
	}
	
	return 0;
}

// get the search length
static DWORD _Everything_GetSearchLengthA(void)
{
	if (_Everything_Search)
	{
		if (_Everything_IsUnicodeSearch)
		{
			return WideCharToMultiByte(CP_ACP,0,(LPCWSTR )_Everything_Search,-1,0,0,0,0);
		}
		else
		{
			return _Everything_StringLengthA((LPCSTR )_Everything_Search);
		}
	}
	
	return 0;
}

// get the search length
static void _Everything_GetSearchTextW(LPWSTR wbuf)
{
	DWORD wlen;
	
	if (_Everything_Search)
	{
		wlen = _Everything_GetSearchLengthW();
			
		if (_Everything_IsUnicodeSearch)
		{
			CopyMemory(wbuf,_Everything_Search,(wlen+1) * sizeof(WCHAR));
			
			return;
		}
		else
		{
			MultiByteToWideChar(CP_ACP,0,(LPCSTR )_Everything_Search,-1,wbuf,wlen+1);
			
			return;
		}
	}

	*wbuf = 0;
}

// get the search length
static void _Everything_GetSearchTextA(LPSTR buf)
{
	DWORD len;
	
	if (_Everything_Search)
	{
		len = _Everything_GetSearchLengthA();
			
		if (_Everything_IsUnicodeSearch)
		{
			WideCharToMultiByte(CP_ACP,0,(LPCWSTR )_Everything_Search,-1,buf,len+1,0,0);
			
			return;
		}
		else
		{
			CopyMemory(buf,_Everything_Search,len+1);
			
			return;
		}
	}

	*buf = 0;
}

static DWORD EVERYTHINGAPI _Everything_query_thread_proc(void *param)
{
	HWND everything_hwnd;

	everything_hwnd = FindWindow(EVERYTHING_IPC_WNDCLASS,0);
	if (everything_hwnd)
	{
		WNDCLASSEX wcex;
		HWND hwnd;
		MSG msg;
		int ret;
		
		ZeroMemory(&wcex,sizeof(WNDCLASSEX));
		wcex.cbSize = sizeof(WNDCLASSEX);
		
		if (!GetClassInfoEx(GetModuleHandle(0),TEXT("EVERYTHING_DLL"),&wcex))
		{
			ZeroMemory(&wcex,sizeof(WNDCLASSEX));
			wcex.cbSize = sizeof(WNDCLASSEX);
			wcex.hInstance = GetModuleHandle(0);
			wcex.lpfnWndProc = _Everything_window_proc;
			wcex.lpszClassName = TEXT("EVERYTHING_DLL");
			
			if (!RegisterClassEx(&wcex))
			{
				_Everything_LastError = EVERYTHING_ERROR_REGISTERCLASSEX;
				
				return 0;
			}
		}
		
//FIXME: this should be static so we keep file info cached.		
		
		hwnd = CreateWindow(
			TEXT("EVERYTHING_DLL"),
			TEXT(""),
			0,
			0,0,0,0,
			0,0,GetModuleHandle(0),0);
			
		if (hwnd)
		{
			_Everything_ChangeWindowMessageFilter(hwnd);
			
			_Everything_ReplyWindow = hwnd;
			_Everything_ReplyID = _EVERYTHING_COPYDATA_QUERYREPLY;
			
			if (_Everything_SendIPCQuery())
			{
				// message pump
loop:

				WaitMessage();
				
				// update windows
				while(PeekMessage(&msg,NULL,0,0,0)) 
				{
					ret = (DWORD)GetMessage(&msg,0,0,0);
					if (ret == -1) goto exit;
					if (!ret) goto exit;
					
					// let windows handle it.
					TranslateMessage(&msg);
					DispatchMessage(&msg);
				}			
				
				goto loop;
			}

exit:

			// get result from window.
			DestroyWindow(hwnd);
		}
		else
		{
			_Everything_LastError = EVERYTHING_ERROR_CREATEWINDOW;
		}
	}
	else
	{
		// the everything window was not found.
		// we can optionally RegisterWindowMessage("EVERYTHING_IPC_CREATED") and 
		// wait for Everything to post this message to all top level windows when its up and running.
		_Everything_LastError = EVERYTHING_ERROR_IPC;
	}

	return 0;
}

static BOOL EVERYTHINGAPI _Everything_Query(void)
{
	HANDLE hthread;
	DWORD thread_id;
	
	// reset the error flag.
	_Everything_LastError = 0;
	
	hthread = CreateThread(0,0,_Everything_query_thread_proc,0,0,&thread_id);
		
	if (hthread)
	{
		WaitForSingleObject(hthread,INFINITE);
		
		CloseHandle(hthread);
	}
	else
	{
		_Everything_LastError = EVERYTHING_ERROR_CREATETHREAD;
	}
	
	return (_Everything_LastError == 0)?TRUE:FALSE;
}

static BOOL _Everything_SendIPCQuery2(HWND everything_hwnd)
{
	BOOL ret;
	DWORD size;
	EVERYTHING_IPC_QUERY2 *query;
		
	// try version 2.
	
	if (_Everything_IsUnicodeQuery)
	{
		// unicode
		size = sizeof(EVERYTHING_IPC_QUERY2) + ((_Everything_GetSearchLengthW() + 1) * sizeof(WCHAR));
	}
	else
	{
		// ansi
		size = sizeof(EVERYTHING_IPC_QUERY2) + ((_Everything_GetSearchLengthA() + 1) * sizeof(char));
	}
	
	// alloc
	query = _Everything_Alloc(size);
	
	if (query)
	{
		COPYDATASTRUCT cds;

		query->max_results = _Everything_Max;
		query->offset = _Everything_Offset;
		query->reply_copydata_message = _Everything_ReplyID;
		query->search_flags = (_Everything_Regex?EVERYTHING_IPC_REGEX:0) | (_Everything_MatchCase?EVERYTHING_IPC_MATCHCASE:0) | (_Everything_MatchWholeWord?EVERYTHING_IPC_MATCHWHOLEWORD:0) | (_Everything_MatchPath?EVERYTHING_IPC_MATCHPATH:0);
		query->reply_hwnd = (DWORD)(DWORD_PTR)_Everything_ReplyWindow;
		query->sort_type = (DWORD)_Everything_Sort;
		query->request_flags = (DWORD)_Everything_RequestFlags;

		if (_Everything_IsUnicodeQuery)
		{
			_Everything_GetSearchTextW((LPWSTR)(query + 1));
		}
		else
		{
			_Everything_GetSearchTextA((LPSTR)(query + 1));
		}

		cds.cbData = size;
		cds.dwData = _Everything_IsUnicodeQuery ? EVERYTHING_IPC_COPYDATA_QUERY2W : EVERYTHING_IPC_COPYDATA_QUERY2A;
		cds.lpData = query;
	
		if (SendMessage(everything_hwnd,WM_COPYDATA,(WPARAM)_Everything_ReplyWindow,(LPARAM)&cds))
		{
			// successful.
			ret = TRUE;
		}
		else
		{
			// no ipc
			_Everything_LastError = EVERYTHING_ERROR_IPC;
			
			ret = FALSE;
		}
		
		// get result from window.
		_Everything_Free(query);
	}
	else
	{
		_Everything_LastError = EVERYTHING_ERROR_MEMORY;
		
		ret = FALSE;
	}
	
	return ret;
}

static BOOL _Everything_ShouldUseVersion2(void)
{
	if (_Everything_RequestFlags != (EVERYTHING_REQUEST_PATH | EVERYTHING_REQUEST_FILE_NAME))
	{
		return TRUE;	
	}

	if (_Everything_Sort != EVERYTHING_SORT_NAME_ASCENDING)
	{
		return TRUE;	
	}
	
	// just use version 1
	return FALSE;
}

static BOOL _Everything_SendIPCQuery(void)
{
	HWND everything_hwnd;
	BOOL ret;
	
		// find the everything ipc window.
	everything_hwnd = FindWindow(EVERYTHING_IPC_WNDCLASS,0);
	if (everything_hwnd)
	{
		_Everything_QueryVersion = 2;
		
		// try version 2 first (if we specified some non-version 1 request flags or sort)
		if ((_Everything_ShouldUseVersion2()) && (_Everything_SendIPCQuery2(everything_hwnd)))
		{
			// sucessful.
			ret = TRUE;		
		}
		else
		{
			DWORD len;
			DWORD size;
			void *query;

			// try version 1.		
			
			if (_Everything_IsUnicodeQuery)
			{
				// unicode
				len = _Everything_GetSearchLengthW();
				
				size = sizeof(EVERYTHING_IPC_QUERYW) - sizeof(WCHAR) + len*sizeof(WCHAR) + sizeof(WCHAR);
			}
			else
			{
				// ansi
				len = _Everything_GetSearchLengthA();
				
				size = sizeof(EVERYTHING_IPC_QUERYA) - sizeof(char) + (len*sizeof(char)) + sizeof(char);
			}
			
			// alloc
			query = _Everything_Alloc(size);
			
			if (query)
			{
				COPYDATASTRUCT cds;
				
				if (_Everything_IsUnicodeQuery)
				{
					((EVERYTHING_IPC_QUERYW *)query)->max_results = _Everything_Max;
					((EVERYTHING_IPC_QUERYW *)query)->offset = _Everything_Offset;
					((EVERYTHING_IPC_QUERYW *)query)->reply_copydata_message = _Everything_ReplyID;
					((EVERYTHING_IPC_QUERYW *)query)->search_flags = (_Everything_Regex?EVERYTHING_IPC_REGEX:0) | (_Everything_MatchCase?EVERYTHING_IPC_MATCHCASE:0) | (_Everything_MatchWholeWord?EVERYTHING_IPC_MATCHWHOLEWORD:0) | (_Everything_MatchPath?EVERYTHING_IPC_MATCHPATH:0);
					((EVERYTHING_IPC_QUERYW *)query)->reply_hwnd = (DWORD)(DWORD_PTR)_Everything_ReplyWindow;

					_Everything_GetSearchTextW(((EVERYTHING_IPC_QUERYW *)query)->search_string);
				}
				else
				{
					((EVERYTHING_IPC_QUERYA *)query)->max_results = _Everything_Max;
					((EVERYTHING_IPC_QUERYA *)query)->offset = _Everything_Offset;
					((EVERYTHING_IPC_QUERYA *)query)->reply_copydata_message = _Everything_ReplyID;
					((EVERYTHING_IPC_QUERYA *)query)->search_flags = (_Everything_Regex?EVERYTHING_IPC_REGEX:0) | (_Everything_MatchCase?EVERYTHING_IPC_MATCHCASE:0) | (_Everything_MatchWholeWord?EVERYTHING_IPC_MATCHWHOLEWORD:0) | (_Everything_MatchPath?EVERYTHING_IPC_MATCHPATH:0);
					((EVERYTHING_IPC_QUERYA *)query)->reply_hwnd = (DWORD)(DWORD_PTR)_Everything_ReplyWindow;
				
					_Everything_GetSearchTextA(((EVERYTHING_IPC_QUERYA *)query)->search_string);
				}

				cds.cbData = size;
				cds.dwData = _Everything_IsUnicodeQuery ? EVERYTHING_IPC_COPYDATAQUERYW : EVERYTHING_IPC_COPYDATAQUERYA;
				cds.lpData = query;
			
				_Everything_QueryVersion = 1;
				
				if (SendMessage(everything_hwnd,WM_COPYDATA,(WPARAM)_Everything_ReplyWindow,(LPARAM)&cds))
				{
					// sucessful.
					ret = TRUE;
				}
				else
				{
					// no ipc
					_Everything_LastError = EVERYTHING_ERROR_IPC;
					
					ret = FALSE;
				}
				
				// get result from window.
				_Everything_Free(query);
			}
			else
			{
				_Everything_LastError = EVERYTHING_ERROR_MEMORY;
				
				ret = FALSE;
			}
		}
	}
	else
	{
		_Everything_LastError = EVERYTHING_ERROR_IPC;
		
		ret = FALSE;
	}

	return ret;
}

BOOL EVERYTHINGAPI Everything_QueryA(BOOL bWait)
{
	BOOL ret;
	
	_Everything_Lock();

	_Everything_IsUnicodeQuery = FALSE;
	
	if (bWait)	
	{
		ret = _Everything_Query();
	}
	else
	{
		ret = _Everything_SendIPCQuery();
	}

	_Everything_Unlock();
	
	return ret;
}

BOOL EVERYTHINGAPI Everything_QueryW(BOOL bWait)
{
	BOOL ret;
	
	_Everything_Lock();
	
	_Everything_IsUnicodeQuery = TRUE;
	
	if (bWait)	
	{
		ret = _Everything_Query();
	}
	else
	{
		ret = _Everything_SendIPCQuery();
	}

	_Everything_Unlock();
	
	return ret;
}

static int __cdecl _Everything_CompareA(const void *a,const void *b)
{
	int i;
	
	i = stricmp(EVERYTHING_IPC_ITEMPATHA(_Everything_List,a),EVERYTHING_IPC_ITEMPATHA(_Everything_List,b));
	
	if (!i)
	{
		return stricmp(EVERYTHING_IPC_ITEMFILENAMEA(_Everything_List,a),EVERYTHING_IPC_ITEMFILENAMEA(_Everything_List,b));
	}
	else
	if (i > 0)
	{
		return 1;
	}
	else
	{
		return -1;
	}
}

static int __cdecl _Everything_CompareW(const void *a,const void *b)
{
	int i;
	
	i = wcsicmp(EVERYTHING_IPC_ITEMPATHW(_Everything_List,a),EVERYTHING_IPC_ITEMPATHW(_Everything_List,b));
	
	if (!i)
	{
		return wcsicmp(EVERYTHING_IPC_ITEMFILENAMEW(_Everything_List,a),EVERYTHING_IPC_ITEMFILENAMEW(_Everything_List,b));
	}
	else
	if (i > 0)
	{
		return 1;
	}
	else
	{
		return -1;
	}
}

void EVERYTHINGAPI Everything_SortResultsByPath(void)
{
	_Everything_Lock();
	
	if (_Everything_List)
	{
		if (_Everything_IsUnicodeQuery)
		{
			qsort(((EVERYTHING_IPC_LISTW *)_Everything_List)->items,((EVERYTHING_IPC_LISTW *)_Everything_List)->numitems,sizeof(EVERYTHING_IPC_ITEMW),_Everything_CompareW);
		}
		else
		{
			qsort(((EVERYTHING_IPC_LISTA *)_Everything_List)->items,((EVERYTHING_IPC_LISTA *)_Everything_List)->numitems,sizeof(EVERYTHING_IPC_ITEMA),_Everything_CompareA);
		}
	}
	else
	{
		_Everything_LastError = EVERYTHING_ERROR_INVALIDCALL;
	}
	
//FIXME://TODO: sort list2

	_Everything_Unlock();
}

DWORD EVERYTHINGAPI Everything_GetLastError(void)
{
	DWORD ret;
		
	_Everything_Lock();
	
	ret = _Everything_LastError;

	_Everything_Unlock();
	
	return ret;
}

DWORD EVERYTHINGAPI Everything_GetNumFileResults(void)
{
	DWORD ret;
	
	_Everything_Lock();

	if (_Everything_List)
	{
		if (_Everything_IsUnicodeQuery)
		{
			ret = ((EVERYTHING_IPC_LISTW *)_Everything_List)->numfiles;
		}
		else
		{
			ret = ((EVERYTHING_IPC_LISTA *)_Everything_List)->numfiles;
		}
	}
	else
	{
		_Everything_LastError = EVERYTHING_ERROR_INVALIDCALL;

		ret = 0;
	}

	_Everything_Unlock();
	
	return ret;
}

DWORD EVERYTHINGAPI Everything_GetNumFolderResults(void)
{
	DWORD ret;

	_Everything_Lock();

	if (_Everything_List)
	{
		if (_Everything_IsUnicodeQuery)
		{
			ret = ((EVERYTHING_IPC_LISTW *)_Everything_List)->numfolders;
		}
		else
		{
			ret = ((EVERYTHING_IPC_LISTA *)_Everything_List)->numfolders;
		}
	}
	else
	{
		_Everything_LastError = EVERYTHING_ERROR_INVALIDCALL;

		ret = 0;
	}

	_Everything_Unlock();
	
	return ret;
}

DWORD EVERYTHINGAPI Everything_GetNumResults(void)
{
	DWORD ret;
	
	_Everything_Lock();

	if (_Everything_List)
	{
		if (_Everything_IsUnicodeQuery)
		{
			ret = ((EVERYTHING_IPC_LISTW *)_Everything_List)->numitems;
		}
		else
		{
			ret = ((EVERYTHING_IPC_LISTA *)_Everything_List)->numitems;
		}
	}
	else
	if (_Everything_List2)
	{
		ret = _Everything_List2->numitems;
	}
	else
	{
		_Everything_LastError = EVERYTHING_ERROR_INVALIDCALL;

		ret = 0;
	}

	_Everything_Unlock();
	
	return ret;
}

DWORD EVERYTHINGAPI Everything_GetTotFileResults(void)
{
	DWORD ret;

	_Everything_Lock();
	
	if (_Everything_List)
	{
		if (_Everything_IsUnicodeQuery)
		{
			ret = ((EVERYTHING_IPC_LISTW *)_Everything_List)->totfiles;
		}
		else
		{
			ret = ((EVERYTHING_IPC_LISTA *)_Everything_List)->totfiles;
		}
	}
	else
	{
		_Everything_LastError = EVERYTHING_ERROR_INVALIDCALL;

		ret = 0;
	}

	_Everything_Unlock();
	
	return ret;
}

DWORD EVERYTHINGAPI Everything_GetTotFolderResults(void)
{
	DWORD ret;

	_Everything_Lock();

	if (_Everything_List)
	{
		if (_Everything_IsUnicodeQuery)
		{
			ret = ((EVERYTHING_IPC_LISTW *)_Everything_List)->totfolders;
		}
		else
		{
			ret = ((EVERYTHING_IPC_LISTA *)_Everything_List)->totfolders;
		}
	}
	else
	{
		_Everything_LastError = EVERYTHING_ERROR_INVALIDCALL;

		ret = 0;
	}

	_Everything_Unlock();
	
	return ret;
}

DWORD EVERYTHINGAPI Everything_GetTotResults(void)
{
	DWORD ret;
	
	_Everything_Lock();

	if (_Everything_List)
	{
		if (_Everything_IsUnicodeQuery)
		{
			ret = ((EVERYTHING_IPC_LISTW *)_Everything_List)->totitems;
		}
		else
		{
			ret = ((EVERYTHING_IPC_LISTA *)_Everything_List)->totitems;
		}
	}
	else
	if (_Everything_List2)
	{
		ret = _Everything_List2->totitems;
	}
	else
	{
		_Everything_LastError = EVERYTHING_ERROR_INVALIDCALL;

		ret = 0;
	}

	_Everything_Unlock();
	
	return ret;
}

BOOL EVERYTHINGAPI Everything_IsVolumeResult(DWORD dwIndex)
{
	BOOL ret;
	
	_Everything_Lock();

	if (_Everything_List)
	{
		if (_Everything_IsValidResultIndex(dwIndex))
		{
			if (_Everything_IsUnicodeQuery)
			{
				ret = (((EVERYTHING_IPC_LISTW *)_Everything_List)->items[dwIndex].flags & EVERYTHING_IPC_DRIVE) ? TRUE : FALSE;
			}
			else
			{
				ret = (((EVERYTHING_IPC_LISTA *)_Everything_List)->items[dwIndex].flags & EVERYTHING_IPC_DRIVE) ? TRUE : FALSE;
			}
		}
		else
		{
			_Everything_LastError = EVERYTHING_ERROR_INVALIDINDEX;
			
			ret = FALSE;
		}
	}
	else
	if (_Everything_List2)
	{
		if (_Everything_IsValidResultIndex(dwIndex))
		{
			ret = (((EVERYTHING_IPC_ITEM2 *)(_Everything_List2 + 1))[dwIndex].flags & EVERYTHING_IPC_DRIVE) ? TRUE : FALSE;
		}
		else
		{
			_Everything_LastError = EVERYTHING_ERROR_INVALIDINDEX;
			
			ret = FALSE;
		}
	}
	else
	{
		_Everything_LastError = EVERYTHING_ERROR_INVALIDCALL;

		ret = FALSE;
	}
	
	_Everything_Unlock();

	return ret;	
}

BOOL EVERYTHINGAPI Everything_IsFolderResult(DWORD dwIndex)
{
	BOOL ret;
	
	_Everything_Lock();

	if (_Everything_List)
	{
		if (_Everything_IsValidResultIndex(dwIndex))
		{
			if (_Everything_IsUnicodeQuery)
			{
				ret = ((EVERYTHING_IPC_LISTW *)_Everything_List)->items[dwIndex].flags & (EVERYTHING_IPC_FOLDER) ? TRUE : FALSE;
			}
			else
			{
				ret = ((EVERYTHING_IPC_LISTA *)_Everything_List)->items[dwIndex].flags & (EVERYTHING_IPC_FOLDER) ? TRUE : FALSE;
			}
		}
		else
		{
			_Everything_LastError = EVERYTHING_ERROR_INVALIDINDEX;
			
			ret = FALSE;
		}
	}
	else
	if (_Everything_List2)
	{
		if (_Everything_IsValidResultIndex(dwIndex))
		{
			ret = (((EVERYTHING_IPC_ITEM2 *)(_Everything_List2 + 1))[dwIndex].flags & (EVERYTHING_IPC_FOLDER)) ? TRUE : FALSE;
		}
		else
		{
			_Everything_LastError = EVERYTHING_ERROR_INVALIDINDEX;
			
			ret = FALSE;
		}
	}
	else
	{
		_Everything_LastError = EVERYTHING_ERROR_INVALIDCALL;

		ret = FALSE;
	}

	_Everything_Unlock();
	
	return ret;
}

BOOL EVERYTHINGAPI Everything_IsFileResult(DWORD dwIndex)
{
	BOOL ret;
	
	_Everything_Lock();

	if (_Everything_List)
	{
		if (_Everything_IsValidResultIndex(dwIndex))
		{
			if (_Everything_IsUnicodeQuery)
			{
				ret = (((EVERYTHING_IPC_LISTW *)_Everything_List)->items[dwIndex].flags & (EVERYTHING_IPC_FOLDER)) ? FALSE : TRUE;
			}
			else
			{
				ret = (((EVERYTHING_IPC_LISTA *)_Everything_List)->items[dwIndex].flags & (EVERYTHING_IPC_FOLDER)) ? FALSE : TRUE;
			}
		}
		else
		{
			_Everything_LastError = EVERYTHING_ERROR_INVALIDINDEX;
			
			ret = FALSE;
		}
	}
	else
	if (_Everything_List2)
	{
		if (_Everything_IsValidResultIndex(dwIndex))
		{
			ret = (((EVERYTHING_IPC_ITEM2 *)(_Everything_List2 + 1))[dwIndex].flags & (EVERYTHING_IPC_FOLDER)) ? FALSE : TRUE;
		}
		else
		{
			_Everything_LastError = EVERYTHING_ERROR_INVALIDINDEX;
			
			ret = FALSE;
		}
	}
	else
	{
		_Everything_LastError = EVERYTHING_ERROR_INVALIDCALL;

		ret = FALSE;
	}
	
	_Everything_Unlock();
	
	return ret;
}

LPCWSTR EVERYTHINGAPI Everything_GetResultFileNameW(DWORD dwIndex)
{
	LPCWSTR ret;
	
	_Everything_Lock();
	
	if ((_Everything_List) && (_Everything_IsUnicodeQuery))
	{
		if (_Everything_IsValidResultIndex(dwIndex))
		{
			ret = EVERYTHING_IPC_ITEMFILENAMEW(_Everything_List,&((EVERYTHING_IPC_LISTW *)_Everything_List)->items[dwIndex]);
		}
		else
		{
			_Everything_LastError = EVERYTHING_ERROR_INVALIDINDEX;
			
			ret = NULL;
		}
	}
	else
	if ((_Everything_List2) && (_Everything_IsUnicodeQuery))
	{
		if (_Everything_IsValidResultIndex(dwIndex))
		{
			ret = _Everything_GetRequestData(dwIndex,EVERYTHING_REQUEST_FILE_NAME);
			
			if (ret)
			{
				// skip length in characters.
				ret = (LPCWSTR)(((char *)ret) + sizeof(DWORD));
			}
			else
			{
				_Everything_LastError = EVERYTHING_ERROR_INVALIDREQUEST;
			}
		}
		else
		{
			_Everything_LastError = EVERYTHING_ERROR_INVALIDINDEX;
			
			ret = NULL;
		}
	}
	else
	{
		_Everything_LastError = EVERYTHING_ERROR_INVALIDCALL;

		ret = NULL;
	}
	
	_Everything_Unlock();

	return ret;
}

LPCSTR EVERYTHINGAPI Everything_GetResultFileNameA(DWORD dwIndex)
{
	LPCSTR ret;
	
	_Everything_Lock();

	if ((_Everything_List) && (!_Everything_IsUnicodeQuery))
	{
		if (_Everything_IsValidResultIndex(dwIndex))
		{
			ret = EVERYTHING_IPC_ITEMFILENAMEA(_Everything_List,&((EVERYTHING_IPC_LISTA *)_Everything_List)->items[dwIndex]);
		}
		else
		{
			_Everything_LastError = EVERYTHING_ERROR_INVALIDINDEX;
			
			ret = NULL;
		}
	}
	else
	if ((_Everything_List2) && (!_Everything_IsUnicodeQuery))
	{
		if (_Everything_IsValidResultIndex(dwIndex))
		{
			ret = _Everything_GetRequestData(dwIndex,EVERYTHING_REQUEST_FILE_NAME);
			
			if (ret)
			{
				// skip length in characters.
				ret = (LPCSTR)(((char *)ret) + sizeof(DWORD));
			}
			else
			{
				_Everything_LastError = EVERYTHING_ERROR_INVALIDREQUEST;
			}
		}
		else
		{
			_Everything_LastError = EVERYTHING_ERROR_INVALIDINDEX;
			
			ret = NULL;
		}
	}
	else
	{
		_Everything_LastError = EVERYTHING_ERROR_INVALIDCALL;

		ret = NULL;
	}
	
	_Everything_Unlock();
	
	return ret;
}

LPCWSTR EVERYTHINGAPI Everything_GetResultPathW(DWORD dwIndex)
{
	LPCWSTR ret;

	_Everything_Lock();
	
	if ((_Everything_List) && (_Everything_IsUnicodeQuery))
	{
		if (_Everything_IsValidResultIndex(dwIndex))
		{
			ret = EVERYTHING_IPC_ITEMPATHW(_Everything_List,&((EVERYTHING_IPC_LISTW *)_Everything_List)->items[dwIndex]);
		}
		else
		{
			_Everything_LastError = EVERYTHING_ERROR_INVALIDINDEX;
			
			ret = NULL;
		}
	}
	else
	if ((_Everything_List2) && (_Everything_IsUnicodeQuery))
	{
		if (_Everything_IsValidResultIndex(dwIndex))
		{
			ret = _Everything_GetRequestData(dwIndex,EVERYTHING_REQUEST_PATH);
			
			if (ret)
			{
				// skip length in characters.
				ret = (LPCWSTR)(((char *)ret) + sizeof(DWORD));
			}
			else
			{
				_Everything_LastError = EVERYTHING_ERROR_INVALIDREQUEST;
			}
		}
		else
		{
			_Everything_LastError = EVERYTHING_ERROR_INVALIDINDEX;
			
			ret = NULL;
		}
	}
	else
	{
		_Everything_LastError = EVERYTHING_ERROR_INVALIDCALL;

		ret = NULL;
	}

	_Everything_Unlock();
	
	return ret;
}

LPCSTR EVERYTHINGAPI Everything_GetResultPathA(DWORD dwIndex)
{
	LPCSTR ret;
	
	_Everything_Lock();

	if (_Everything_List)
	{
		if (_Everything_IsValidResultIndex(dwIndex))
		{
			ret = EVERYTHING_IPC_ITEMPATHA(_Everything_List,&((EVERYTHING_IPC_LISTA *)_Everything_List)->items[dwIndex]);
		}
		else
		{
			_Everything_LastError = EVERYTHING_ERROR_INVALIDINDEX;
			
			ret = NULL;
		}
	}
	else
	if ((_Everything_List2) && (!_Everything_IsUnicodeQuery))
	{
		if (_Everything_IsValidResultIndex(dwIndex))
		{
			ret = _Everything_GetRequestData(dwIndex,EVERYTHING_REQUEST_PATH);
			
			if (ret)
			{
				// skip length in characters.
				ret = (LPCSTR)(((char *)ret) + sizeof(DWORD));
			}
			else
			{
				_Everything_LastError = EVERYTHING_ERROR_INVALIDREQUEST;
			}
		}
		else
		{
			_Everything_LastError = EVERYTHING_ERROR_INVALIDINDEX;
			
			ret = NULL;
		}
	}
	else
	{
		_Everything_LastError = EVERYTHING_ERROR_INVALIDCALL;

		ret = NULL;
	}

	_Everything_Unlock();
	
	return ret;
}

// max is in chars
static DWORD _Everything_CopyW(LPWSTR buf,DWORD bufmax,DWORD catlen,LPCWSTR s)
{
	DWORD wlen;

	if (buf)
	{
		buf += catlen;
		bufmax -= catlen;
	}
	
	wlen = _Everything_StringLengthW(s);
	if (!wlen) 
	{
		if (buf)
		{
			buf[wlen] = 0;
		}
	
		return catlen;
	}

	// terminate
	if (wlen > bufmax-1) wlen = bufmax-1;

	if (buf)
	{
		CopyMemory(buf,s,wlen*sizeof(WCHAR));

		buf[wlen] = 0;
	}
	
	return wlen + catlen;
}

static DWORD _Everything_CopyA(LPSTR buf,DWORD max,DWORD catlen,LPCSTR s)
{
	DWORD len;
	
	if (buf)
	{
		buf += catlen;
		max -= catlen;
	}
	
	len = _Everything_StringLengthA(s);
	if (!len) 
	{
		if (buf)
		{
			buf[len] = 0;
		}
	
		return catlen;
	}

	// terminate
	if (len > max-1) len = max-1;

	if (buf)
	{
		CopyMemory(buf,s,len*sizeof(char));

		buf[len] = 0;
	}
	
	return len + catlen;

}

// max is in chars
static DWORD _Everything_CopyWFromA(LPWSTR buf,DWORD bufmax,DWORD catlen,LPCSTR s)
{
	DWORD wlen;

	if (buf)
	{
		buf += catlen;
		bufmax -= catlen;
	}
	
	wlen = MultiByteToWideChar(CP_ACP,0,s,_Everything_StringLengthA(s),0,0);
	if (!wlen) 
	{
		if (buf)
		{
			buf[wlen] = 0;
		}
	
		return catlen;
	}

	// terminate
	if (wlen > bufmax-1) wlen = bufmax-1;

	if (buf)
	{
		MultiByteToWideChar(CP_ACP,0,s,_Everything_StringLengthA(s),buf,wlen);

		buf[wlen] = 0;
	}
	
	return wlen + catlen;
}

static DWORD _Everything_CopyAFromW(LPSTR buf,DWORD max,DWORD catlen,LPCWSTR s)
{
	DWORD len;
	
	if (buf)
	{
		buf += catlen;
		max -= catlen;
	}
	
	len = WideCharToMultiByte(CP_ACP,0,s,_Everything_StringLengthW(s),0,0,0,0);
	if (!len) 
	{
		if (buf)
		{
			buf[len] = 0;
		}
	
		return catlen;
	}

	// terminate
	if (len > max-1) len = max-1;

	if (buf)
	{
		WideCharToMultiByte(CP_ACP,0,s,_Everything_StringLengthW(s),buf,len,0,0);

		buf[len] = 0;
	}
	
	return len + catlen;

}

DWORD EVERYTHINGAPI Everything_GetResultFullPathNameW(DWORD dwIndex,LPWSTR wbuf,DWORD wbuf_size_in_wchars)
{
	DWORD len;

	_Everything_Lock();
	
	if (_Everything_List)
	{
		if (_Everything_IsValidResultIndex(dwIndex))
		{
			if (_Everything_IsUnicodeQuery)		
			{
				len = _Everything_CopyW(wbuf,wbuf_size_in_wchars,0,EVERYTHING_IPC_ITEMPATHW(_Everything_List,&((EVERYTHING_IPC_LISTW *)_Everything_List)->items[dwIndex]));

				if (len)
				{
					len = _Everything_CopyW(wbuf,wbuf_size_in_wchars,len,_Everything_IsSchemeNameW(EVERYTHING_IPC_ITEMPATHW(_Everything_List,&((EVERYTHING_IPC_LISTW *)_Everything_List)->items[dwIndex])) ? L"/" : L"\\");
				}
			}
			else
			{
				len = _Everything_CopyWFromA(wbuf,wbuf_size_in_wchars,0,EVERYTHING_IPC_ITEMPATHA(_Everything_List,&((EVERYTHING_IPC_LISTA *)_Everything_List)->items[dwIndex]));
				
				if (len)
				{
					len = _Everything_CopyW(wbuf,wbuf_size_in_wchars,len,_Everything_IsSchemeNameA(EVERYTHING_IPC_ITEMPATHA(_Everything_List,&((EVERYTHING_IPC_LISTA *)_Everything_List)->items[dwIndex])) ? L"/" : L"\\");
				}
			}

			if (_Everything_IsUnicodeQuery)		
			{
				len = _Everything_CopyW(wbuf,wbuf_size_in_wchars,len,EVERYTHING_IPC_ITEMFILENAMEW(_Everything_List,&((EVERYTHING_IPC_LISTW *)_Everything_List)->items[dwIndex]));
			}
			else
			{
				len = _Everything_CopyWFromA(wbuf,wbuf_size_in_wchars,len,EVERYTHING_IPC_ITEMFILENAMEA(_Everything_List,&((EVERYTHING_IPC_LISTA *)_Everything_List)->items[dwIndex]));
			}
		}
		else
		{
			_Everything_LastError = EVERYTHING_ERROR_INVALIDINDEX;
			
			len = _Everything_CopyW(wbuf,wbuf_size_in_wchars,0,L"");
		}
	}
	else
	if (_Everything_List2)
	{
		if (_Everything_IsValidResultIndex(dwIndex))
		{
			const void *full_path_and_name;
			
			full_path_and_name = _Everything_GetRequestData(dwIndex,EVERYTHING_REQUEST_FULL_PATH_AND_FILE_NAME);
			
			if (full_path_and_name)
			{
				// skip number of characters.
				full_path_and_name = (void *)(((char *)full_path_and_name) + sizeof(DWORD));

				// we got the full path and name already.
				if (_Everything_IsUnicodeQuery)		
				{
					len = _Everything_CopyW(wbuf,wbuf_size_in_wchars,0,full_path_and_name);
				}
				else
				{
					len = _Everything_CopyWFromA(wbuf,wbuf_size_in_wchars,0,full_path_and_name);
				}
			}
			else
			{
				const void *path;
				
				path = _Everything_GetRequestData(dwIndex,EVERYTHING_REQUEST_PATH);
				
				if (path)
				{
					const void *name;

					// skip number of characters.
					path = (void *)(((char *)path) + sizeof(DWORD));
					
					name = _Everything_GetRequestData(dwIndex,EVERYTHING_REQUEST_FILE_NAME);
					
					if (name)
					{
						// skip number of characters.
						name = (void *)(((char *)name) + sizeof(DWORD));

						if (_Everything_IsUnicodeQuery)		
						{
							len = _Everything_CopyW(wbuf,wbuf_size_in_wchars,0,path);

							if (len)
							{
								len = _Everything_CopyW(wbuf,wbuf_size_in_wchars,len,_Everything_IsSchemeNameW(path) ? L"/" : L"\\");
							}
						}
						else
						{
							len = _Everything_CopyWFromA(wbuf,wbuf_size_in_wchars,0,path);

							if (len)
							{
								len = _Everything_CopyW(wbuf,wbuf_size_in_wchars,len,_Everything_IsSchemeNameA(path) ? L"/" : L"\\");
							}
						}

						if (_Everything_IsUnicodeQuery)		
						{
							len = _Everything_CopyW(wbuf,wbuf_size_in_wchars,len,name);
						}
						else
						{
							len = _Everything_CopyWFromA(wbuf,wbuf_size_in_wchars,len,name);
						}						
					}
					else
					{
						// name data not available.
						_Everything_LastError = EVERYTHING_ERROR_INVALIDREQUEST;
						
						len = _Everything_CopyW(wbuf,wbuf_size_in_wchars,0,L"");
					}
				}
				else
				{
					// path data not available.
					_Everything_LastError = EVERYTHING_ERROR_INVALIDREQUEST;
					
					len = _Everything_CopyW(wbuf,wbuf_size_in_wchars,0,L"");
				}
			}
		}
		else
		{
			_Everything_LastError = EVERYTHING_ERROR_INVALIDINDEX;
			
			len = _Everything_CopyW(wbuf,wbuf_size_in_wchars,0,L"");
		}
	}
	else
	{
		_Everything_LastError = EVERYTHING_ERROR_INVALIDCALL;

		len = _Everything_CopyW(wbuf,wbuf_size_in_wchars,0,L"");
	}

	_Everything_Unlock();
	
	return len;
}

DWORD EVERYTHINGAPI Everything_GetResultFullPathNameA(DWORD dwIndex,LPSTR buf,DWORD bufsize)
{
	DWORD len;
	
	_Everything_Lock();

	if (_Everything_List)
	{
		if (_Everything_IsValidResultIndex(dwIndex))
		{
			if (_Everything_IsUnicodeQuery)		
			{
				len = _Everything_CopyAFromW(buf,bufsize,0,EVERYTHING_IPC_ITEMPATHW(_Everything_List,&((EVERYTHING_IPC_LISTW *)_Everything_List)->items[dwIndex]));
			}
			else
			{
				len = _Everything_CopyA(buf,bufsize,0,EVERYTHING_IPC_ITEMPATHA(_Everything_List,&((EVERYTHING_IPC_LISTA *)_Everything_List)->items[dwIndex]));
			}
			
			if (len)
			{
				len = _Everything_CopyA(buf,bufsize,len,_Everything_IsSchemeNameA(buf) ? "/" : "\\");
			}

			if (_Everything_IsUnicodeQuery)		
			{
				len = _Everything_CopyAFromW(buf,bufsize,len,EVERYTHING_IPC_ITEMFILENAMEW(_Everything_List,&((EVERYTHING_IPC_LISTW *)_Everything_List)->items[dwIndex]));
			}
			else
			{
				len = _Everything_CopyA(buf,bufsize,len,EVERYTHING_IPC_ITEMFILENAMEA(_Everything_List,&((EVERYTHING_IPC_LISTA *)_Everything_List)->items[dwIndex]));
			}
		}
		else
		{
			_Everything_LastError = EVERYTHING_ERROR_INVALIDINDEX;
			
			len = _Everything_CopyA(buf,bufsize,0,"");
		}
	}
	else
	if (_Everything_List2)
	{
		if (_Everything_IsValidResultIndex(dwIndex))
		{
			const void *full_path_and_name;
			
			full_path_and_name = _Everything_GetRequestData(dwIndex,EVERYTHING_REQUEST_FULL_PATH_AND_FILE_NAME);
			
			if (full_path_and_name)
			{
				// skip number of characters.
				full_path_and_name = (void *)(((char *)full_path_and_name) + sizeof(DWORD));
				
				// we got the full path and name already.
				if (_Everything_IsUnicodeQuery)		
				{
					len = _Everything_CopyAFromW(buf,bufsize,0,full_path_and_name);
				}
				else
				{
					len = _Everything_CopyA(buf,bufsize,0,full_path_and_name);
				}
			}
			else
			{
				const void *path;
				
				path = _Everything_GetRequestData(dwIndex,EVERYTHING_REQUEST_PATH);
				
				if (path)
				{
					const void *name;

					// skip number of characters.
					path = (void *)(((char *)path) + sizeof(DWORD));
					
					name = _Everything_GetRequestData(dwIndex,EVERYTHING_REQUEST_FILE_NAME);
					
					if (name)
					{
						// skip number of characters.
						name = (void *)(((char *)name) + sizeof(DWORD));

						if (_Everything_IsUnicodeQuery)		
						{
							len = _Everything_CopyAFromW(buf,bufsize,0,path);
						}
						else
						{
							len = _Everything_CopyA(buf,bufsize,0,path);
						}
							
						if (len)
						{
							len = _Everything_CopyA(buf,bufsize,len,_Everything_IsSchemeNameA(buf) ? "/" : "\\");
						}

						if (_Everything_IsUnicodeQuery)		
						{
							len = _Everything_CopyAFromW(buf,bufsize,len,name);
						}
						else
						{
							len = _Everything_CopyA(buf,bufsize,len,name);
						}						
					}
					else
					{
						// name data not available.
						_Everything_LastError = EVERYTHING_ERROR_INVALIDREQUEST;
						
						len = _Everything_CopyA(buf,bufsize,0,"");
					}
				}
				else
				{
					// path data not available.
					_Everything_LastError = EVERYTHING_ERROR_INVALIDREQUEST;
					
					len = _Everything_CopyA(buf,bufsize,0,"");
				}
			}
		}
		else
		{
			_Everything_LastError = EVERYTHING_ERROR_INVALIDINDEX;
			
			len = _Everything_CopyA(buf,bufsize,0,"");
		}
	}
	else
	{
		_Everything_LastError = EVERYTHING_ERROR_INVALIDCALL;

		len = _Everything_CopyA(buf,bufsize,0,"");
	}

	_Everything_Unlock();
	
	return len;
}

BOOL EVERYTHINGAPI Everything_IsQueryReply(UINT message,WPARAM wParam,LPARAM lParam,DWORD dwId)
{
	if (message == WM_COPYDATA)
	{
		COPYDATASTRUCT *cds = (COPYDATASTRUCT *)lParam;
		
		if (cds)
		{
			if ((cds->dwData == _Everything_ReplyID) && (cds->dwData == dwId))
			{
				if (_Everything_QueryVersion == 2)
				{
					_Everything_FreeLists();

					_Everything_List2 = _Everything_Alloc(cds->cbData);
						
					if (_Everything_List2)
					{
						_Everything_LastError = 0;
						
						CopyMemory(_Everything_List2,cds->lpData,cds->cbData);
					}
					else
					{
						_Everything_LastError = EVERYTHING_ERROR_MEMORY;
					}
					
					return TRUE;
				}
				else
				if (_Everything_QueryVersion == 1)
				{
					if (_Everything_IsUnicodeQuery)				
					{
						_Everything_FreeLists();

						_Everything_List = _Everything_Alloc(cds->cbData);
						
						if (_Everything_List)
						{
							_Everything_LastError = 0;
							
							CopyMemory(_Everything_List,cds->lpData,cds->cbData);
						}
						else
						{
							_Everything_LastError = EVERYTHING_ERROR_MEMORY;
						}
						
						return TRUE;
					}
					else
					{
						_Everything_FreeLists();
						
						_Everything_List = _Everything_Alloc(cds->cbData);
						
						if (_Everything_List)
						{
							_Everything_LastError = 0;
							
							CopyMemory(_Everything_List,cds->lpData,cds->cbData);
						}
						else
						{
							_Everything_LastError = EVERYTHING_ERROR_MEMORY;
						}

						return TRUE;
					}
				}
			}
		}
	}
	
	return FALSE;
}

void EVERYTHINGAPI Everything_Reset(void)
{
	_Everything_Lock();
	
	if (_Everything_Search)
	{
		_Everything_Free(_Everything_Search);
		
		_Everything_Search = 0;
	}
	
	_Everything_FreeLists();

	// reset state
	_Everything_MatchPath = FALSE;
	_Everything_MatchCase = FALSE;
	_Everything_MatchWholeWord = FALSE;
	_Everything_Regex = FALSE;
	_Everything_LastError = FALSE;
	_Everything_Max = EVERYTHING_IPC_ALLRESULTS;
	_Everything_Offset = 0;
	_Everything_Sort = EVERYTHING_SORT_NAME_ASCENDING;
	_Everything_RequestFlags = EVERYTHING_REQUEST_PATH | EVERYTHING_REQUEST_FILE_NAME;
	_Everything_IsUnicodeQuery = FALSE;
	_Everything_IsUnicodeSearch = FALSE;

	_Everything_Unlock();
}

void EVERYTHINGAPI Everything_CleanUp(void)
{
	Everything_Reset();
	DeleteCriticalSection(&_Everything_cs);
	_Everything_Initialized = 0;
}

static void *_Everything_Alloc(DWORD size)
{
	return HeapAlloc(GetProcessHeap(),0,size);
}

static void _Everything_Free(void *ptr)
{
	HeapFree(GetProcessHeap(),0,ptr);
}

EVERYTHINGUSERAPI DWORD EVERYTHINGAPI Everything_GetResultListSort(void)
{
	DWORD dwSort;
	
	_Everything_Lock();
	
	dwSort = EVERYTHING_SORT_NAME_ASCENDING;
	
	if (_Everything_List2)
	{
		dwSort = _Everything_List2->sort_type;
	}

	_Everything_Unlock();	
	
	return dwSort;
}

EVERYTHINGUSERAPI DWORD EVERYTHINGAPI Everything_GetResultListRequestFlags(void)
{
	DWORD dwRequestFlags;
	
	_Everything_Lock();
	
	dwRequestFlags = EVERYTHING_REQUEST_PATH | EVERYTHING_REQUEST_FILE_NAME;
	
	if (_Everything_List2)
	{
		dwRequestFlags = _Everything_List2->request_flags;
	}

	_Everything_Unlock();	
	
	return dwRequestFlags;
}

static void _Everything_FreeLists(void)
{
	if (_Everything_List)
	{
		_Everything_Free(_Everything_List);
		
		_Everything_List = 0;
	}

	if (_Everything_List2)
	{
		_Everything_Free(_Everything_List2);
		
		_Everything_List2 = 0;
	}
}

static BOOL _Everything_IsValidResultIndex(DWORD dwIndex)
{
	if (dwIndex < 0)
	{
		return FALSE;
	}
	
	if (dwIndex >= Everything_GetNumResults())
	{
		return FALSE;
	}
	
	return TRUE;
}

// assumes _Everything_List2 and dwIndex are valid.
static void *_Everything_GetRequestData(DWORD dwIndex,DWORD dwRequestType)
{
	char *p;
	EVERYTHING_IPC_ITEM2 *items;
	
	items = (EVERYTHING_IPC_ITEM2 *)(_Everything_List2 + 1);
	
	p = ((char *)_Everything_List2) + items[dwIndex].data_offset;
	
	if (_Everything_List2->request_flags & EVERYTHING_REQUEST_FILE_NAME)
	{
		DWORD len;

		if (dwRequestType == EVERYTHING_REQUEST_FILE_NAME)	
		{
			return p;
		}
		
		len = *(DWORD *)p;
		p += sizeof(DWORD);
		
		if (_Everything_IsUnicodeQuery)
		{
			p += (len + 1) * sizeof(WCHAR);
		}
		else
		{
			p += (len + 1) * sizeof(CHAR);
		}
	}		
	
	if (_Everything_List2->request_flags & EVERYTHING_REQUEST_PATH)
	{
		DWORD len;
		
		if (dwRequestType == EVERYTHING_REQUEST_PATH)	
		{
			return p;
		}
		
		len = *(DWORD *)p;
		p += sizeof(DWORD);
		
		if (_Everything_IsUnicodeQuery)
		{
			p += (len + 1) * sizeof(WCHAR);
		}
		else
		{
			p += (len + 1) * sizeof(CHAR);
		}
	}
	
	if (_Everything_List2->request_flags & EVERYTHING_REQUEST_FULL_PATH_AND_FILE_NAME)
	{
		DWORD len;
		
		if (dwRequestType == EVERYTHING_REQUEST_FULL_PATH_AND_FILE_NAME)	
		{
			return p;
		}
		
		len = *(DWORD *)p;
		p += sizeof(DWORD);

		if (_Everything_IsUnicodeQuery)
		{
			p += (len + 1) * sizeof(WCHAR);
		}
		else
		{
			p += (len + 1) * sizeof(CHAR);
		}
	}
	
	if (_Everything_List2->request_flags & EVERYTHING_REQUEST_EXTENSION)
	{
		DWORD len;
		
		if (dwRequestType == EVERYTHING_REQUEST_EXTENSION)	
		{
			return p;
		}
		
		len = *(DWORD *)p;
		p += sizeof(DWORD);
		
		if (_Everything_IsUnicodeQuery)
		{
			p += (len + 1) * sizeof(WCHAR);
		}
		else
		{
			p += (len + 1) * sizeof(CHAR);
		}
	}
	
	if (_Everything_List2->request_flags & EVERYTHING_REQUEST_SIZE)
	{
		if (dwRequestType == EVERYTHING_REQUEST_SIZE)	
		{
			return p;
		}
		
		p += sizeof(LARGE_INTEGER);
	}
	
	if (_Everything_List2->request_flags & EVERYTHING_REQUEST_DATE_CREATED)
	{
		if (dwRequestType == EVERYTHING_REQUEST_DATE_CREATED)	
		{
			return p;
		}
		
		p += sizeof(FILETIME);
	}
	
	if (_Everything_List2->request_flags & EVERYTHING_REQUEST_DATE_MODIFIED)
	{
		if (dwRequestType == EVERYTHING_REQUEST_DATE_MODIFIED)	
		{
			return p;
		}
		
		p += sizeof(FILETIME);
	}
	
	if (_Everything_List2->request_flags & EVERYTHING_REQUEST_DATE_ACCESSED)
	{
		if (dwRequestType == EVERYTHING_REQUEST_DATE_ACCESSED)	
		{
			return p;
		}
		
		p += sizeof(FILETIME);
	}
	
	if (_Everything_List2->request_flags & EVERYTHING_REQUEST_ATTRIBUTES)
	{
		if (dwRequestType == EVERYTHING_REQUEST_ATTRIBUTES)	
		{
			return p;
		}
		
		p += sizeof(DWORD);
	}
		
	if (_Everything_List2->request_flags & EVERYTHING_REQUEST_FILE_LIST_FILE_NAME)
	{
		DWORD len;
		
		if (dwRequestType == EVERYTHING_REQUEST_FILE_LIST_FILE_NAME)	
		{
			return p;
		}
		
		len = *(DWORD *)p;
		p += sizeof(DWORD);
		
		if (_Everything_IsUnicodeQuery)
		{
			p += (len + 1) * sizeof(WCHAR);
		}
		else
		{
			p += (len + 1) * sizeof(CHAR);
		}
	}	
		
	if (_Everything_List2->request_flags & EVERYTHING_REQUEST_RUN_COUNT)
	{
		if (dwRequestType == EVERYTHING_REQUEST_RUN_COUNT)	
		{
			return p;
		}
		
		p += sizeof(DWORD);
	}	
	
	if (_Everything_List2->request_flags & EVERYTHING_REQUEST_DATE_RUN)
	{
		if (dwRequestType == EVERYTHING_REQUEST_DATE_RUN)	
		{
			return p;
		}
		
		p += sizeof(FILETIME);
	}		
	
	if (_Everything_List2->request_flags & EVERYTHING_REQUEST_DATE_RECENTLY_CHANGED)
	{
		if (dwRequestType == EVERYTHING_REQUEST_DATE_RECENTLY_CHANGED)	
		{
			return p;
		}
		
		p += sizeof(FILETIME);
	}	
	
	if (_Everything_List2->request_flags & EVERYTHING_REQUEST_HIGHLIGHTED_FILE_NAME)
	{
		DWORD len;
		
		if (dwRequestType == EVERYTHING_REQUEST_HIGHLIGHTED_FILE_NAME)	
		{
			return p;
		}
		
		len = *(DWORD *)p;
		p += sizeof(DWORD);
		
		if (_Everything_IsUnicodeQuery)
		{
			p += (len + 1) * sizeof(WCHAR);
		}
		else
		{
			p += (len + 1) * sizeof(CHAR);
		}
	}		
	
	if (_Everything_List2->request_flags & EVERYTHING_REQUEST_HIGHLIGHTED_PATH)
	{
		DWORD len;
		
		if (dwRequestType == EVERYTHING_REQUEST_HIGHLIGHTED_PATH)	
		{
			return p;
		}
		
		len = *(DWORD *)p;
		p += sizeof(DWORD);
		
		if (_Everything_IsUnicodeQuery)
		{
			p += (len + 1) * sizeof(WCHAR);
		}
		else
		{
			p += (len + 1) * sizeof(CHAR);
		}
	}
	
	if (_Everything_List2->request_flags & EVERYTHING_REQUEST_HIGHLIGHTED_FULL_PATH_AND_FILE_NAME)
	{
		DWORD len;
		
		if (dwRequestType == EVERYTHING_REQUEST_HIGHLIGHTED_FULL_PATH_AND_FILE_NAME)	
		{
			return p;
		}
		
		len = *(DWORD *)p;
		p += sizeof(DWORD);
		
		if (_Everything_IsUnicodeQuery)
		{
			p += (len + 1) * sizeof(WCHAR);
		}
		else
		{
			p += (len + 1) * sizeof(CHAR);
		}
	}			
	
	return NULL;
}

static BOOL _Everything_IsSchemeNameW(LPCWSTR s)
{
	LPCWSTR p;
	
	p = s;

	while(*p)
	{
		if (*p == ':')
		{
			p++;
			
			if ((p[0] == '/') && (p[1] == '/'))
			{
				return TRUE;
			}
			
			break;
		}
		
		p++;
	}
	
	return FALSE;
}

static BOOL _Everything_IsSchemeNameA(LPCSTR s)
{
	LPCSTR p;
	
	p = s;

	while(*p)
	{
		if (*p == ':')
		{
			p++;
			
			if ((p[0] == '/') && (p[1] == '/'))
			{
				return TRUE;
			}
			
			break;
		}
		
		p++;
	}
	
	return FALSE;
}

static void _Everything_ChangeWindowMessageFilter(HWND hwnd)
{
	if (!_Everything_GotChangeWindowMessageFilterEx)
	{
		// allow the everything window to send a reply.
		_Everything_user32_hdll = LoadLibraryW(L"user32.dll");
		
		if (_Everything_user32_hdll)
		{
			_Everything_pChangeWindowMessageFilterEx = (BOOL (WINAPI *)(HWND hWnd,UINT message,DWORD action,_EVERYTHING_PCHANGEFILTERSTRUCT pChangeFilterStruct))GetProcAddress(_Everything_user32_hdll,"ChangeWindowMessageFilterEx");
		}
	
		_Everything_GotChangeWindowMessageFilterEx = 1;
	}

	if (_Everything_GotChangeWindowMessageFilterEx)
	{
		if (_Everything_pChangeWindowMessageFilterEx)
		{
			_Everything_pChangeWindowMessageFilterEx(hwnd,WM_COPYDATA,_EVERYTHING_MSGFLT_ALLOW,0);
		}
	}
}

static LPCWSTR _Everything_GetResultRequestStringW(DWORD dwIndex,DWORD dwRequestType)
{
	LPCWSTR str;
	
	_Everything_Lock();

	if ((_Everything_List2) && (_Everything_IsUnicodeQuery))
	{
		if (_Everything_IsValidResultIndex(dwIndex))
		{
			str = _Everything_GetRequestData(dwIndex,dwRequestType);
			if (str)
			{
				// skip length in characters.
				str = (LPCWSTR)(((char *)str) + sizeof(DWORD));
			}
			else
			{
				_Everything_LastError = EVERYTHING_ERROR_INVALIDREQUEST;
			}
		}
		else
		{
			_Everything_LastError = EVERYTHING_ERROR_INVALIDINDEX;

			str = NULL;
		}
	}
	else
	{
		_Everything_LastError = EVERYTHING_ERROR_INVALIDCALL;

		str = NULL;
	}
	
	_Everything_Unlock();	
	
	return str;
}

static LPCSTR _Everything_GetResultRequestStringA(DWORD dwIndex,DWORD dwRequestType)
{
	LPCSTR str;
	
	_Everything_Lock();

	if ((_Everything_List2) && (!_Everything_IsUnicodeQuery))
	{
		if (_Everything_IsValidResultIndex(dwIndex))
		{
			str = _Everything_GetRequestData(dwIndex,dwRequestType);
			if (str)
			{
				// skip length in characters.
				str = (LPCSTR)(((char *)str) + sizeof(DWORD));
			}
			else
			{
				_Everything_LastError = EVERYTHING_ERROR_INVALIDREQUEST;
			}
		}
		else
		{
			_Everything_LastError = EVERYTHING_ERROR_INVALIDINDEX;

			str = NULL;
		}
	}
	else
	{
		_Everything_LastError = EVERYTHING_ERROR_INVALIDCALL;

		str = NULL;
	}
	
	_Everything_Unlock();	
	
	return str;
}

static BOOL _Everything_GetResultRequestData(DWORD dwIndex,DWORD dwRequestType,void *data,int size)
{
	BOOL ret;
	
	_Everything_Lock();

	if (_Everything_List2)
	{
		if (_Everything_IsValidResultIndex(dwIndex))
		{
			void *request_data;
			
			request_data = _Everything_GetRequestData(dwIndex,dwRequestType);
			if (request_data)
			{
				CopyMemory(data,request_data,size);
				
				ret = TRUE;
			}
			else
			{
				_Everything_LastError = EVERYTHING_ERROR_INVALIDREQUEST;

				ret = FALSE;
			}
		}
		else
		{
			_Everything_LastError = EVERYTHING_ERROR_INVALIDINDEX;

			ret = FALSE;
		}
	}
	else
	{
		_Everything_LastError = EVERYTHING_ERROR_INVALIDCALL;

		ret = FALSE;
	}
	
	_Everything_Unlock();	
	
	return ret;
}

LPCWSTR EVERYTHINGAPI Everything_GetResultExtensionW(DWORD dwIndex)
{
	return _Everything_GetResultRequestStringW(dwIndex,EVERYTHING_REQUEST_EXTENSION);
}

LPCSTR EVERYTHINGAPI Everything_GetResultExtensionA(DWORD dwIndex)
{
	return _Everything_GetResultRequestStringA(dwIndex,EVERYTHING_REQUEST_EXTENSION);
}

BOOL EVERYTHINGAPI Everything_GetResultSize(DWORD dwIndex,LARGE_INTEGER *lpSize)
{
	return _Everything_GetResultRequestData(dwIndex,EVERYTHING_REQUEST_SIZE,lpSize,sizeof(LARGE_INTEGER));
}

BOOL EVERYTHINGAPI Everything_GetResultDateCreated(DWORD dwIndex,FILETIME *lpDateCreated)
{
	return _Everything_GetResultRequestData(dwIndex,EVERYTHING_REQUEST_DATE_CREATED,lpDateCreated,sizeof(FILETIME));
}

BOOL EVERYTHINGAPI Everything_GetResultDateModified(DWORD dwIndex,FILETIME *lpDateModified)
{
	return _Everything_GetResultRequestData(dwIndex,EVERYTHING_REQUEST_DATE_MODIFIED,lpDateModified,sizeof(FILETIME));
}

BOOL EVERYTHINGAPI Everything_GetResultDateAccessed(DWORD dwIndex,FILETIME *lpDateAccessed)
{
	return _Everything_GetResultRequestData(dwIndex,EVERYTHING_REQUEST_DATE_ACCESSED,lpDateAccessed,sizeof(FILETIME));
}

DWORD EVERYTHINGAPI Everything_GetResultAttributes(DWORD dwIndex)
{
	DWORD dwAttributes;
	
	if (_Everything_GetResultRequestData(dwIndex,EVERYTHING_REQUEST_ATTRIBUTES,&dwAttributes,sizeof(DWORD)))
	{
		return dwAttributes;
	}

	return INVALID_FILE_ATTRIBUTES;
}

LPCWSTR EVERYTHINGAPI Everything_GetResultFileListFileNameW(DWORD dwIndex)
{
	return _Everything_GetResultRequestStringW(dwIndex,EVERYTHING_REQUEST_FILE_LIST_FILE_NAME);
}

LPCSTR EVERYTHINGAPI Everything_GetResultFileListFileNameA(DWORD dwIndex)
{
	return _Everything_GetResultRequestStringA(dwIndex,EVERYTHING_REQUEST_FILE_LIST_FILE_NAME);
}

DWORD EVERYTHINGAPI Everything_GetResultRunCount(DWORD dwIndex)
{
	DWORD dwRunCount;
	
	if (_Everything_GetResultRequestData(dwIndex,EVERYTHING_REQUEST_RUN_COUNT,&dwRunCount,sizeof(DWORD)))
	{
		return dwRunCount;
	}

	return 0;	
}

BOOL EVERYTHINGAPI Everything_GetResultDateRun(DWORD dwIndex,FILETIME *lpDateRun)
{
	return _Everything_GetResultRequestData(dwIndex,EVERYTHING_REQUEST_DATE_RUN,lpDateRun,sizeof(FILETIME));
}

BOOL EVERYTHINGAPI Everything_GetResultDateRecentlyChanged(DWORD dwIndex,FILETIME *lpDateRecentlyChanged)
{
	return _Everything_GetResultRequestData(dwIndex,EVERYTHING_REQUEST_DATE_RECENTLY_CHANGED,lpDateRecentlyChanged,sizeof(FILETIME));
}

LPCWSTR EVERYTHINGAPI Everything_GetResultHighlightedFileNameW(DWORD dwIndex)
{
	return _Everything_GetResultRequestStringW(dwIndex,EVERYTHING_REQUEST_HIGHLIGHTED_FILE_NAME);
}

LPCSTR EVERYTHINGAPI Everything_GetResultHighlightedFileNameA(DWORD dwIndex)
{
	return _Everything_GetResultRequestStringA(dwIndex,EVERYTHING_REQUEST_HIGHLIGHTED_FILE_NAME);
}

LPCWSTR EVERYTHINGAPI Everything_GetResultHighlightedPathW(DWORD dwIndex)
{
	return _Everything_GetResultRequestStringW(dwIndex,EVERYTHING_REQUEST_HIGHLIGHTED_PATH);
}

LPCSTR EVERYTHINGAPI Everything_GetResultHighlightedPathA(DWORD dwIndex)
{
	return _Everything_GetResultRequestStringA(dwIndex,EVERYTHING_REQUEST_HIGHLIGHTED_PATH);
}

LPCWSTR EVERYTHINGAPI Everything_GetResultHighlightedFullPathAndFileNameW(DWORD dwIndex)
{
	return _Everything_GetResultRequestStringW(dwIndex,EVERYTHING_REQUEST_HIGHLIGHTED_FULL_PATH_AND_FILE_NAME);
}

LPCSTR EVERYTHINGAPI Everything_GetResultHighlightedFullPathAndFileNameA(DWORD dwIndex)
{
	return _Everything_GetResultRequestStringA(dwIndex,EVERYTHING_REQUEST_HIGHLIGHTED_FULL_PATH_AND_FILE_NAME);
}

static BOOL _Everything_SendAPIBoolCommand(int command,LPARAM lParam)
{
	HWND everything_hwnd;
	
	everything_hwnd = FindWindow(EVERYTHING_IPC_WNDCLASS,0);
	if (everything_hwnd)
	{
		_Everything_LastError = 0;
			
		if (SendMessage(everything_hwnd,EVERYTHING_WM_IPC,command,lParam))
		{
			return TRUE;
		}
		else
		{
			return FALSE;
		}
	}
	else
	{
		// the everything window was not found.
		// we can optionally RegisterWindowMessage("EVERYTHING_IPC_CREATED") and 
		// wait for Everything to post this message to all top level windows when its up and running.
		_Everything_LastError = EVERYTHING_ERROR_IPC;
		
		return FALSE;
	}
}

static DWORD _Everything_SendAPIDwordCommand(int command,LPARAM lParam)
{
	HWND everything_hwnd;
	
	everything_hwnd = FindWindow(EVERYTHING_IPC_WNDCLASS,0);
	if (everything_hwnd)
	{
		_Everything_LastError = 0;
		
		return (DWORD)SendMessage(everything_hwnd,EVERYTHING_WM_IPC,command,lParam);
	}
	else
	{
		// the everything window was not found.
		// we can optionally RegisterWindowMessage("EVERYTHING_IPC_CREATED") and 
		// wait for Everything to post this message to all top level windows when its up and running.
		_Everything_LastError = EVERYTHING_ERROR_IPC;
		
		return 0;
	}
}

BOOL EVERYTHINGAPI Everything_IsDBLoaded(void)
{
	return _Everything_SendAPIBoolCommand(EVERYTHING_IPC_IS_DB_LOADED,0);
}

BOOL EVERYTHINGAPI Everything_IsAdmin(void)
{
	return _Everything_SendAPIBoolCommand(EVERYTHING_IPC_IS_ADMIN,0);
}

BOOL EVERYTHINGAPI Everything_IsAppData(void)
{
	return _Everything_SendAPIBoolCommand(EVERYTHING_IPC_IS_APPDATA,0);
}

BOOL EVERYTHINGAPI Everything_RebuildDB(void)
{
	return _Everything_SendAPIBoolCommand(EVERYTHING_IPC_REBUILD_DB,0);
}

BOOL EVERYTHINGAPI Everything_UpdateAllFolderIndexes(void)
{
	return _Everything_SendAPIBoolCommand(EVERYTHING_IPC_UPDATE_ALL_FOLDER_INDEXES,0);
}

BOOL EVERYTHINGAPI Everything_SaveDB(void)
{
	return _Everything_SendAPIBoolCommand(EVERYTHING_IPC_SAVE_DB,0);
}

BOOL EVERYTHINGAPI Everything_SaveRunHistory(void)
{
	return _Everything_SendAPIBoolCommand(EVERYTHING_IPC_SAVE_RUN_HISTORY,0);
}

BOOL EVERYTHINGAPI Everything_DeleteRunHistory(void)
{
	return _Everything_SendAPIBoolCommand(EVERYTHING_IPC_DELETE_RUN_HISTORY,0);
}

DWORD EVERYTHINGAPI Everything_GetMajorVersion(void)
{
	return _Everything_SendAPIDwordCommand(EVERYTHING_IPC_GET_MAJOR_VERSION,0);
}

DWORD EVERYTHINGAPI Everything_GetMinorVersion(void)
{
	return _Everything_SendAPIDwordCommand(EVERYTHING_IPC_GET_MINOR_VERSION,0);
}

DWORD EVERYTHINGAPI Everything_GetRevision(void)
{
	return _Everything_SendAPIDwordCommand(EVERYTHING_IPC_GET_REVISION,0);
}

DWORD EVERYTHINGAPI Everything_GetBuildNumber(void)
{
	return _Everything_SendAPIDwordCommand(EVERYTHING_IPC_GET_BUILD_NUMBER,0);
}

DWORD EVERYTHINGAPI Everything_GetTargetMachine(void)
{
	return _Everything_SendAPIDwordCommand(EVERYTHING_IPC_GET_TARGET_MACHINE,0);
}

BOOL EVERYTHINGAPI Everything_Exit(void)
{
	return _Everything_SendAPIBoolCommand(EVERYTHING_IPC_EXIT,0);
}

BOOL EVERYTHINGAPI Everything_IsFastSort(DWORD sortType)
{
	return _Everything_SendAPIBoolCommand(EVERYTHING_IPC_IS_FAST_SORT,(LPARAM)sortType);
}

BOOL EVERYTHINGAPI Everything_IsFileInfoIndexed(DWORD fileInfoType)
{
	return _Everything_SendAPIBoolCommand(EVERYTHING_IPC_IS_FILE_INFO_INDEXED,(LPARAM)fileInfoType);
}

static LRESULT _Everything_SendCopyData(int command,const void *data,int size)
{
	HWND everything_hwnd;
	
	everything_hwnd = FindWindow(EVERYTHING_IPC_WNDCLASS,0);
	if (everything_hwnd)
	{
		COPYDATASTRUCT cds;

		cds.cbData = size;
		cds.dwData = command;
		cds.lpData = (void *)data;

		return SendMessage(everything_hwnd,WM_COPYDATA,0,(LPARAM)&cds);
	}
	else
	{
		// the everything window was not found.
		// we can optionally RegisterWindowMessage("EVERYTHING_IPC_CREATED") and 
		// wait for Everything to post this message to all top level windows when its up and running.
		_Everything_LastError = EVERYTHING_ERROR_IPC;
		
		return FALSE;
	}
}

DWORD EVERYTHINGAPI Everything_GetRunCountFromFileNameW(LPCWSTR lpFileName)
{
	return (DWORD)_Everything_SendCopyData(EVERYTHING_IPC_COPYDATA_GET_RUN_COUNTW,lpFileName,(_Everything_StringLengthW(lpFileName) + 1) * sizeof(WCHAR));
}

DWORD EVERYTHINGAPI Everything_GetRunCountFromFileNameA(LPCSTR lpFileName)
{
	return (DWORD)_Everything_SendCopyData(EVERYTHING_IPC_COPYDATA_GET_RUN_COUNTA,lpFileName,_Everything_StringLengthA(lpFileName) + 1);
}

BOOL EVERYTHINGAPI Everything_SetRunCountFromFileNameW(LPCWSTR lpFileName,DWORD dwRunCount)
{
	EVERYTHING_IPC_RUN_HISTORY *run_history;
	DWORD len;
	BOOL ret;
	
	len = _Everything_StringLengthW(lpFileName);
	
	run_history = _Everything_Alloc(sizeof(EVERYTHING_IPC_RUN_HISTORY) + ((len + 1) * sizeof(WCHAR)));
	
	if (run_history)
	{
		run_history->run_count = dwRunCount;
		CopyMemory(run_history + 1,lpFileName,((len + 1) * sizeof(WCHAR)));
	
		if (_Everything_SendCopyData(EVERYTHING_IPC_COPYDATA_SET_RUN_COUNTW,run_history,sizeof(EVERYTHING_IPC_RUN_HISTORY) + ((len + 1) * sizeof(WCHAR))))
		{
			ret = TRUE;
		}
		else
		{
			_Everything_LastError = EVERYTHING_ERROR_INVALIDCALL;
			
			ret = FALSE;
		}
		
		_Everything_Free(run_history);
	}
	else
	{
		_Everything_LastError = EVERYTHING_ERROR_MEMORY;
	
		ret = FALSE;
	}	
	
	return ret;
}

BOOL EVERYTHINGAPI Everything_SetRunCountFromFileNameA(LPCSTR lpFileName,DWORD dwRunCount)
{
	EVERYTHING_IPC_RUN_HISTORY *run_history;
	DWORD len;
	BOOL ret;
	
	len = _Everything_StringLengthA(lpFileName);
	
	run_history = _Everything_Alloc(sizeof(EVERYTHING_IPC_RUN_HISTORY) + (len + 1));
	
	if (run_history)
	{
		run_history->run_count = dwRunCount;
		CopyMemory(run_history + 1,lpFileName,(len + 1));
	
		if (_Everything_SendCopyData(EVERYTHING_IPC_COPYDATA_SET_RUN_COUNTA,run_history,sizeof(EVERYTHING_IPC_RUN_HISTORY) + (len + 1)))
		{
			ret = TRUE;
		}
		else
		{
			_Everything_LastError = EVERYTHING_ERROR_INVALIDCALL;
			
			ret = FALSE;
		}
		
		_Everything_Free(run_history);
	}
	else
	{
		_Everything_LastError = EVERYTHING_ERROR_MEMORY;
	
		ret = FALSE;
	}	
	
	return ret;
}

DWORD EVERYTHINGAPI Everything_IncRunCountFromFileNameW(LPCWSTR lpFileName)
{
	return (DWORD)_Everything_SendCopyData(EVERYTHING_IPC_COPYDATA_INC_RUN_COUNTW,lpFileName,(_Everything_StringLengthW(lpFileName) + 1) * sizeof(WCHAR));
}

DWORD EVERYTHINGAPI Everything_IncRunCountFromFileNameA(LPCSTR lpFileName)
{
	return (DWORD)_Everything_SendCopyData(EVERYTHING_IPC_COPYDATA_INC_RUN_COUNTA,lpFileName,_Everything_StringLengthA(lpFileName) + 1);
}

