
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

#ifndef _EVERYTHING_DLL_
#define _EVERYTHING_DLL_

#ifndef _INC_WINDOWS
#include <windows.h>
#endif

#ifdef __cplusplus
extern "C" {
#endif

#define EVERYTHING_OK						0 // no error detected
#define EVERYTHING_ERROR_MEMORY				1 // out of memory.
#define EVERYTHING_ERROR_IPC				2 // Everything search client is not running
#define EVERYTHING_ERROR_REGISTERCLASSEX	3 // unable to register window class.
#define EVERYTHING_ERROR_CREATEWINDOW		4 // unable to create listening window
#define EVERYTHING_ERROR_CREATETHREAD		5 // unable to create listening thread
#define EVERYTHING_ERROR_INVALIDINDEX		6 // invalid index
#define EVERYTHING_ERROR_INVALIDCALL		7 // invalid call
#define EVERYTHING_ERROR_INVALIDREQUEST		8 // invalid request data, request data first.
#define EVERYTHING_ERROR_INVALIDPARAMETER	9 // bad parameter.

#define EVERYTHING_SORT_NAME_ASCENDING						1
#define EVERYTHING_SORT_NAME_DESCENDING						2
#define EVERYTHING_SORT_PATH_ASCENDING						3
#define EVERYTHING_SORT_PATH_DESCENDING						4
#define EVERYTHING_SORT_SIZE_ASCENDING						5
#define EVERYTHING_SORT_SIZE_DESCENDING						6
#define EVERYTHING_SORT_EXTENSION_ASCENDING					7
#define EVERYTHING_SORT_EXTENSION_DESCENDING				8
#define EVERYTHING_SORT_TYPE_NAME_ASCENDING					9
#define EVERYTHING_SORT_TYPE_NAME_DESCENDING				10
#define EVERYTHING_SORT_DATE_CREATED_ASCENDING				11
#define EVERYTHING_SORT_DATE_CREATED_DESCENDING				12
#define EVERYTHING_SORT_DATE_MODIFIED_ASCENDING				13
#define EVERYTHING_SORT_DATE_MODIFIED_DESCENDING			14
#define EVERYTHING_SORT_ATTRIBUTES_ASCENDING				15
#define EVERYTHING_SORT_ATTRIBUTES_DESCENDING				16
#define EVERYTHING_SORT_FILE_LIST_FILENAME_ASCENDING		17
#define EVERYTHING_SORT_FILE_LIST_FILENAME_DESCENDING		18
#define EVERYTHING_SORT_RUN_COUNT_ASCENDING					19
#define EVERYTHING_SORT_RUN_COUNT_DESCENDING				20
#define EVERYTHING_SORT_DATE_RECENTLY_CHANGED_ASCENDING		21
#define EVERYTHING_SORT_DATE_RECENTLY_CHANGED_DESCENDING	22
#define EVERYTHING_SORT_DATE_ACCESSED_ASCENDING				23
#define EVERYTHING_SORT_DATE_ACCESSED_DESCENDING			24
#define EVERYTHING_SORT_DATE_RUN_ASCENDING					25
#define EVERYTHING_SORT_DATE_RUN_DESCENDING					26

#define EVERYTHING_REQUEST_FILE_NAME						0x00000001
#define EVERYTHING_REQUEST_PATH								0x00000002
#define EVERYTHING_REQUEST_FULL_PATH_AND_FILE_NAME			0x00000004
#define EVERYTHING_REQUEST_EXTENSION						0x00000008
#define EVERYTHING_REQUEST_SIZE								0x00000010
#define EVERYTHING_REQUEST_DATE_CREATED						0x00000020
#define EVERYTHING_REQUEST_DATE_MODIFIED					0x00000040
#define EVERYTHING_REQUEST_DATE_ACCESSED					0x00000080
#define EVERYTHING_REQUEST_ATTRIBUTES						0x00000100
#define EVERYTHING_REQUEST_FILE_LIST_FILE_NAME				0x00000200
#define EVERYTHING_REQUEST_RUN_COUNT						0x00000400
#define EVERYTHING_REQUEST_DATE_RUN							0x00000800
#define EVERYTHING_REQUEST_DATE_RECENTLY_CHANGED			0x00001000
#define EVERYTHING_REQUEST_HIGHLIGHTED_FILE_NAME			0x00002000
#define EVERYTHING_REQUEST_HIGHLIGHTED_PATH					0x00004000
#define EVERYTHING_REQUEST_HIGHLIGHTED_FULL_PATH_AND_FILE_NAME	0x00008000

#define EVERYTHING_TARGET_MACHINE_X86						1
#define EVERYTHING_TARGET_MACHINE_X64						2
#define EVERYTHING_TARGET_MACHINE_ARM						3

#ifndef EVERYTHINGAPI
#define EVERYTHINGAPI __stdcall
#endif

#ifndef EVERYTHINGUSERAPI
#define EVERYTHINGUSERAPI __declspec(dllimport)
#endif

// write search state
EVERYTHINGUSERAPI void EVERYTHINGAPI Everything_SetSearchW(LPCWSTR lpString);
EVERYTHINGUSERAPI void EVERYTHINGAPI Everything_SetSearchA(LPCSTR lpString);
EVERYTHINGUSERAPI void EVERYTHINGAPI Everything_SetMatchPath(BOOL bEnable);
EVERYTHINGUSERAPI void EVERYTHINGAPI Everything_SetMatchCase(BOOL bEnable);
EVERYTHINGUSERAPI void EVERYTHINGAPI Everything_SetMatchWholeWord(BOOL bEnable);
EVERYTHINGUSERAPI void EVERYTHINGAPI Everything_SetRegex(BOOL bEnable);
EVERYTHINGUSERAPI void EVERYTHINGAPI Everything_SetMax(DWORD dwMax);
EVERYTHINGUSERAPI void EVERYTHINGAPI Everything_SetOffset(DWORD dwOffset);
EVERYTHINGUSERAPI void EVERYTHINGAPI Everything_SetReplyWindow(HWND hWnd);
EVERYTHINGUSERAPI void EVERYTHINGAPI Everything_SetReplyID(DWORD dwId);
EVERYTHINGUSERAPI void EVERYTHINGAPI Everything_SetSort(DWORD dwSort); // Everything 1.4.1
EVERYTHINGUSERAPI void EVERYTHINGAPI Everything_SetRequestFlags(DWORD dwRequestFlags); // Everything 1.4.1

// read search state
EVERYTHINGUSERAPI BOOL EVERYTHINGAPI Everything_GetMatchPath(void);
EVERYTHINGUSERAPI BOOL EVERYTHINGAPI Everything_GetMatchCase(void);
EVERYTHINGUSERAPI BOOL EVERYTHINGAPI Everything_GetMatchWholeWord(void);
EVERYTHINGUSERAPI BOOL EVERYTHINGAPI Everything_GetRegex(void);
EVERYTHINGUSERAPI DWORD EVERYTHINGAPI Everything_GetMax(void);
EVERYTHINGUSERAPI DWORD EVERYTHINGAPI Everything_GetOffset(void);
EVERYTHINGUSERAPI LPCSTR EVERYTHINGAPI Everything_GetSearchA(void);
EVERYTHINGUSERAPI LPCWSTR EVERYTHINGAPI Everything_GetSearchW(void);
EVERYTHINGUSERAPI DWORD EVERYTHINGAPI Everything_GetLastError(void);
EVERYTHINGUSERAPI HWND EVERYTHINGAPI Everything_GetReplyWindow(void);
EVERYTHINGUSERAPI DWORD EVERYTHINGAPI Everything_GetReplyID(void);
EVERYTHINGUSERAPI DWORD EVERYTHINGAPI Everything_GetSort(void); // Everything 1.4.1
EVERYTHINGUSERAPI DWORD EVERYTHINGAPI Everything_GetRequestFlags(void); // Everything 1.4.1

// execute query
EVERYTHINGUSERAPI BOOL EVERYTHINGAPI Everything_QueryA(BOOL bWait);
EVERYTHINGUSERAPI BOOL EVERYTHINGAPI Everything_QueryW(BOOL bWait);

// query reply
EVERYTHINGUSERAPI BOOL EVERYTHINGAPI Everything_IsQueryReply(UINT message,WPARAM wParam,LPARAM lParam,DWORD dwId);

// write result state
EVERYTHINGUSERAPI void EVERYTHINGAPI Everything_SortResultsByPath(void);

// read result state
EVERYTHINGUSERAPI DWORD EVERYTHINGAPI Everything_GetNumFileResults(void);
EVERYTHINGUSERAPI DWORD EVERYTHINGAPI Everything_GetNumFolderResults(void);
EVERYTHINGUSERAPI DWORD EVERYTHINGAPI Everything_GetNumResults(void);
EVERYTHINGUSERAPI DWORD EVERYTHINGAPI Everything_GetTotFileResults(void);
EVERYTHINGUSERAPI DWORD EVERYTHINGAPI Everything_GetTotFolderResults(void);
EVERYTHINGUSERAPI DWORD EVERYTHINGAPI Everything_GetTotResults(void);
EVERYTHINGUSERAPI BOOL EVERYTHINGAPI Everything_IsVolumeResult(DWORD dwIndex);
EVERYTHINGUSERAPI BOOL EVERYTHINGAPI Everything_IsFolderResult(DWORD dwIndex);
EVERYTHINGUSERAPI BOOL EVERYTHINGAPI Everything_IsFileResult(DWORD dwIndex);
EVERYTHINGUSERAPI LPCWSTR EVERYTHINGAPI Everything_GetResultFileNameW(DWORD dwIndex);
EVERYTHINGUSERAPI LPCSTR EVERYTHINGAPI Everything_GetResultFileNameA(DWORD dwIndex);
EVERYTHINGUSERAPI LPCWSTR EVERYTHINGAPI Everything_GetResultPathW(DWORD dwIndex);
EVERYTHINGUSERAPI LPCSTR EVERYTHINGAPI Everything_GetResultPathA(DWORD dwIndex);
EVERYTHINGUSERAPI DWORD EVERYTHINGAPI Everything_GetResultFullPathNameA(DWORD dwIndex,LPSTR buf,DWORD bufsize);
EVERYTHINGUSERAPI DWORD EVERYTHINGAPI Everything_GetResultFullPathNameW(DWORD dwIndex,LPWSTR wbuf,DWORD wbuf_size_in_wchars);
EVERYTHINGUSERAPI DWORD EVERYTHINGAPI Everything_GetResultListSort(void); // Everything 1.4.1
EVERYTHINGUSERAPI DWORD EVERYTHINGAPI Everything_GetResultListRequestFlags(void); // Everything 1.4.1
EVERYTHINGUSERAPI LPCWSTR EVERYTHINGAPI Everything_GetResultExtensionW(DWORD dwIndex); // Everything 1.4.1
EVERYTHINGUSERAPI LPCSTR EVERYTHINGAPI Everything_GetResultExtensionA(DWORD dwIndex); // Everything 1.4.1
EVERYTHINGUSERAPI BOOL EVERYTHINGAPI Everything_GetResultSize(DWORD dwIndex,LARGE_INTEGER *lpSize); // Everything 1.4.1
EVERYTHINGUSERAPI BOOL EVERYTHINGAPI Everything_GetResultDateCreated(DWORD dwIndex,FILETIME *lpDateCreated); // Everything 1.4.1
EVERYTHINGUSERAPI BOOL EVERYTHINGAPI Everything_GetResultDateModified(DWORD dwIndex,FILETIME *lpDateModified); // Everything 1.4.1
EVERYTHINGUSERAPI BOOL EVERYTHINGAPI Everything_GetResultDateAccessed(DWORD dwIndex,FILETIME *lpDateAccessed); // Everything 1.4.1
EVERYTHINGUSERAPI DWORD EVERYTHINGAPI Everything_GetResultAttributes(DWORD dwIndex); // Everything 1.4.1
EVERYTHINGUSERAPI LPCWSTR EVERYTHINGAPI Everything_GetResultFileListFileNameW(DWORD dwIndex); // Everything 1.4.1
EVERYTHINGUSERAPI LPCSTR EVERYTHINGAPI Everything_GetResultFileListFileNameA(DWORD dwIndex); // Everything 1.4.1
EVERYTHINGUSERAPI DWORD EVERYTHINGAPI Everything_GetResultRunCount(DWORD dwIndex); // Everything 1.4.1
EVERYTHINGUSERAPI BOOL EVERYTHINGAPI Everything_GetResultDateRun(DWORD dwIndex,FILETIME *lpDateRun);
EVERYTHINGUSERAPI BOOL EVERYTHINGAPI Everything_GetResultDateRecentlyChanged(DWORD dwIndex,FILETIME *lpDateRecentlyChanged);
EVERYTHINGUSERAPI LPCWSTR EVERYTHINGAPI Everything_GetResultHighlightedFileNameW(DWORD dwIndex); // Everything 1.4.1
EVERYTHINGUSERAPI LPCSTR EVERYTHINGAPI Everything_GetResultHighlightedFileNameA(DWORD dwIndex); // Everything 1.4.1
EVERYTHINGUSERAPI LPCWSTR EVERYTHINGAPI Everything_GetResultHighlightedPathW(DWORD dwIndex); // Everything 1.4.1
EVERYTHINGUSERAPI LPCSTR EVERYTHINGAPI Everything_GetResultHighlightedPathA(DWORD dwIndex); // Everything 1.4.1
EVERYTHINGUSERAPI LPCWSTR EVERYTHINGAPI Everything_GetResultHighlightedFullPathAndFileNameW(DWORD dwIndex); // Everything 1.4.1
EVERYTHINGUSERAPI LPCSTR EVERYTHINGAPI Everything_GetResultHighlightedFullPathAndFileNameA(DWORD dwIndex); // Everything 1.4.1

// reset state and free any allocated memory
EVERYTHINGUSERAPI void EVERYTHINGAPI Everything_Reset(void);
EVERYTHINGUSERAPI void EVERYTHINGAPI Everything_CleanUp(void);

EVERYTHINGUSERAPI DWORD EVERYTHINGAPI Everything_GetMajorVersion(void);
EVERYTHINGUSERAPI DWORD EVERYTHINGAPI Everything_GetMinorVersion(void);
EVERYTHINGUSERAPI DWORD EVERYTHINGAPI Everything_GetRevision(void);
EVERYTHINGUSERAPI DWORD EVERYTHINGAPI Everything_GetBuildNumber(void);
EVERYTHINGUSERAPI BOOL EVERYTHINGAPI Everything_Exit(void);
EVERYTHINGUSERAPI BOOL EVERYTHINGAPI Everything_IsDBLoaded(void); // Everything 1.4.1
EVERYTHINGUSERAPI BOOL EVERYTHINGAPI Everything_IsAdmin(void); // Everything 1.4.1
EVERYTHINGUSERAPI BOOL EVERYTHINGAPI Everything_IsAppData(void); // Everything 1.4.1
EVERYTHINGUSERAPI BOOL EVERYTHINGAPI Everything_RebuildDB(void); // Everything 1.4.1
EVERYTHINGUSERAPI BOOL EVERYTHINGAPI Everything_UpdateAllFolderIndexes(void); // Everything 1.4.1
EVERYTHINGUSERAPI BOOL EVERYTHINGAPI Everything_SaveDB(void); // Everything 1.4.1
EVERYTHINGUSERAPI BOOL EVERYTHINGAPI Everything_SaveRunHistory(void); // Everything 1.4.1
EVERYTHINGUSERAPI BOOL EVERYTHINGAPI Everything_DeleteRunHistory(void); // Everything 1.4.1
EVERYTHINGUSERAPI DWORD EVERYTHINGAPI Everything_GetTargetMachine(void); // Everything 1.4.1

EVERYTHINGUSERAPI DWORD EVERYTHINGAPI Everything_GetRunCountFromFileNameW(LPCWSTR lpFileName); // Everything 1.4.1
EVERYTHINGUSERAPI DWORD EVERYTHINGAPI Everything_GetRunCountFromFileNameA(LPCSTR lpFileName); // Everything 1.4.1
EVERYTHINGUSERAPI BOOL EVERYTHINGAPI Everything_SetRunCountFromFileNameW(LPCWSTR lpFileName,DWORD dwRunCount); // Everything 1.4.1
EVERYTHINGUSERAPI BOOL EVERYTHINGAPI Everything_SetRunCountFromFileNameA(LPCSTR lpFileName,DWORD dwRunCount); // Everything 1.4.1
EVERYTHINGUSERAPI DWORD EVERYTHINGAPI Everything_IncRunCountFromFileNameW(LPCWSTR lpFileName); // Everything 1.4.1
EVERYTHINGUSERAPI DWORD EVERYTHINGAPI Everything_IncRunCountFromFileNameA(LPCSTR lpFileName); // Everything 1.4.1

#ifdef UNICODE
#define Everything_SetSearch Everything_SetSearchW
#define Everything_GetSearch Everything_GetSearchW
#define Everything_Query Everything_QueryW
#define Everything_Query2 Everything_Query2W
#define Everything_GetResultFileName Everything_GetResultFileNameW
#define Everything_GetResultPath Everything_GetResultPathW
#define Everything_GetResultFullPathName Everything_GetResultFullPathNameW
#define Everything_GetResultExtension Everything_GetResultExtensionW
#define Everything_GetResultFileListFileName Everything_GetResultFileListFileNameW
#define Everything_GetResultHighlightedFileName Everything_GetResultHighlightedFileNameW
#define Everything_GetResultHighlightedPath Everything_GetResultHighlightedPathW
#define Everything_GetResultHighlightedFullPathAndFileName Everything_GetResultHighlightedFullPathAndFileNameW
#define Everything_GetRunCountFromFileName Everything_GetRunCountFromFileNameW
#define Everything_SetRunCountFromFileName Everything_SetRunCountFromFileNameW
#define Everything_IncRunCountFromFileName Everything_IncRunCountFromFileNameW
#else
#define Everything_SetSearch Everything_SetSearchA
#define Everything_GetSearch Everything_GetSearchA
#define Everything_Query Everything_QueryA
#define Everything_Query2 Everything_Query2A
#define Everything_GetResultFileName Everything_GetResultFileNameA
#define Everything_GetResultPath Everything_GetResultPathA
#define Everything_GetResultFullPathName Everything_GetResultFullPathNameA
#define Everything_GetResultExtension Everything_GetResultExtensionA
#define Everything_GetResultFileListFileName Everything_GetResultFileListFileNameA
#define Everything_GetResultHighlightedFileName Everything_GetResultHighlightedFileNameA
#define Everything_GetResultHighlightedPath Everything_GetResultHighlightedPathA
#define Everything_GetResultHighlightedFullPathAndFileName Everything_GetResultHighlightedFullPathAndFileNameA
#define Everything_GetRunCountFromFileName Everything_GetRunCountFromFileNameA
#define Everything_SetRunCountFromFileName Everything_SetRunCountFromFileNameA
#define Everything_IncRunCountFromFileName Everything_IncRunCountFromFileNameA
#endif

#ifdef __cplusplus
}
#endif

#endif

