
// Everything IPC

#ifndef _EVERYTHING_IPC_H_
#define _EVERYTHING_IPC_H_

// C
#ifdef __cplusplus
extern "C" {
#endif

#define EVERYTHING_WM_IPC												(WM_USER)

#define EVERYTHING_IPC_TARGET_MACHINE_X86								1
#define EVERYTHING_IPC_TARGET_MACHINE_X64								2
#define EVERYTHING_IPC_TARGET_MACHINE_ARM								3
#define EVERYTHING_IPC_TARGET_MACHINE_ARM64								4

// built in filters
#define EVERYTHING_IPC_FILTER_EVERYTHING								0
#define EVERYTHING_IPC_FILTER_AUDIO										1
#define EVERYTHING_IPC_FILTER_COMPRESSED								2
#define EVERYTHING_IPC_FILTER_DOCUMENT									3
#define EVERYTHING_IPC_FILTER_EXECUTABLE								4
#define EVERYTHING_IPC_FILTER_FOLDER									5
#define EVERYTHING_IPC_FILTER_PICTURE									6
#define EVERYTHING_IPC_FILTER_VIDEO										7
#define EVERYTHING_IPC_FILTER_CUSTOM									8

// EVERYTHING_WM_IPC (send to the Everything taskbar notification window)
// the Everything taskbar notification window is always created when Everything is running. (even when the taskbar notification icon is hidden)
// HWND everything_taskbar_notification_hwnd = FindWindow(EVERYTHING_IPC_WNDCLASS,0);
// SendMessage(everything_taskbar_notification_hwnd,EVERYTHING_WM_IPC,EVERYTHING_IPC_*,lParam)

// version format: major.minor.revision.build 
// example: 1.4.1.877
#define EVERYTHING_IPC_GET_MAJOR_VERSION								0 // int major_version = (int)SendMessage(everything_taskbar_notification_hwnd,EVERYTHING_WM_IPC,EVERYTHING_IPC_GET_MAJOR_VERSION,0);
#define EVERYTHING_IPC_GET_MINOR_VERSION								1 // int minor_version = (int)SendMessage(everything_taskbar_notification_hwnd,EVERYTHING_WM_IPC,EVERYTHING_IPC_GET_MINOR_VERSION,0);
#define EVERYTHING_IPC_GET_REVISION										2 // int revision = (int)SendMessage(everything_taskbar_notification_hwnd,EVERYTHING_WM_IPC,EVERYTHING_IPC_GET_REVISION,0);
#define EVERYTHING_IPC_GET_BUILD_NUMBER									3 // int build = (int)SendMessage(everything_taskbar_notification_hwnd,EVERYTHING_WM_IPC,EVERYTHING_IPC_GET_BUILD,0);
#define EVERYTHING_IPC_EXIT												4 // returns 1 if the program closes.
#define EVERYTHING_IPC_GET_TARGET_MACHINE								5 // int target_machine = (int)SendMessage(everything_taskbar_notification_hwnd,EVERYTHING_WM_IPC,EVERYTHING_IPC_GET_TARGET_MACHINE,0); returns 0 if not supported. returns a EVERYTHING_IPC_TARGET_MACHINE_* value. requires Everything 1.4.1

// uninstall options
#define EVERYTHING_IPC_DELETE_START_MENU_SHORTCUTS						100 // SendMessage(everything_taskbar_notification_hwnd,EVERYTHING_WM_IPC,EVERYTHING_IPC_DELETE_START_MENU_SHORTCUTS,0);
#define EVERYTHING_IPC_DELETE_QUICK_LAUNCH_SHORTCUT						101 // SendMessage(everything_taskbar_notification_hwnd,EVERYTHING_WM_IPC,EVERYTHING_IPC_DELETE_QUICK_LAUNCH_SHORTCUT,0);
#define EVERYTHING_IPC_DELETE_DESKTOP_SHORTCUT							102 // SendMessage(everything_taskbar_notification_hwnd,EVERYTHING_WM_IPC,EVERYTHING_IPC_DELETE_DESKTOP_SHORTCUT,0);
#define EVERYTHING_IPC_DELETE_FOLDER_CONTEXT_MENU						103 // SendMessage(everything_taskbar_notification_hwnd,EVERYTHING_WM_IPC,EVERYTHING_IPC_DELETE_FOLDER_CONTEXT_MENU,0);
#define EVERYTHING_IPC_DELETE_RUN_ON_SYSTEM_STARTUP						104 // SendMessage(everything_taskbar_notification_hwnd,EVERYTHING_WM_IPC,EVERYTHING_IPC_DELETE_RUN_ON_SYSTEM_STARTUP,0);
#define EVERYTHING_IPC_DELETE_URL_PROTOCOL								105 // SendMessage(everything_taskbar_notification_hwnd,EVERYTHING_WM_IPC,EVERYTHING_IPC_DELETE_URL_PROTOCOL,0);

// install options
#define EVERYTHING_IPC_CREATE_START_MENU_SHORTCUTS						200 // SendMessage(everything_taskbar_notification_hwnd,EVERYTHING_WM_IPC,EVERYTHING_IPC_CREATE_START_MENU_SHORTCUTS,0);
#define EVERYTHING_IPC_CREATE_QUICK_LAUNCH_SHORTCUT						201 // SendMessage(everything_taskbar_notification_hwnd,EVERYTHING_WM_IPC,EVERYTHING_IPC_CREATE_QUICK_LAUNCH_SHORTCUT,0);
#define EVERYTHING_IPC_CREATE_DESKTOP_SHORTCUT							202 // SendMessage(everything_taskbar_notification_hwnd,EVERYTHING_WM_IPC,EVERYTHING_IPC_CREATE_DESKTOP_SHORTCUT,0);
#define EVERYTHING_IPC_CREATE_FOLDER_CONTEXT_MENU						203 // SendMessage(everything_taskbar_notification_hwnd,EVERYTHING_WM_IPC,EVERYTHING_IPC_CREATE_FOLDER_CONTEXT_MENU,0);
#define EVERYTHING_IPC_CREATE_RUN_ON_SYSTEM_STARTUP						204 // SendMessage(everything_taskbar_notification_hwnd,EVERYTHING_WM_IPC,EVERYTHING_IPC_CREATE_RUN_ON_SYSTEM_STARTUP,0);
#define EVERYTHING_IPC_CREATE_URL_PROTOCOL								205 // SendMessage(everything_taskbar_notification_hwnd,EVERYTHING_WM_IPC,EVERYTHING_IPC_CREATE_URL_PROTOCOL,0);

// get option status; 0 = no, 1 = yes, 2 = indeterminate (partially installed)
#define EVERYTHING_IPC_IS_START_MENU_SHORTCUTS							300 // int ret = (int)SendMessage(everything_taskbar_notification_hwnd,EVERYTHING_WM_IPC,EVERYTHING_IPC_IS_START_MENU_SHORTCUTS,0);
#define EVERYTHING_IPC_IS_QUICK_LAUNCH_SHORTCUT							301 // int ret = (int)SendMessage(everything_taskbar_notification_hwnd,EVERYTHING_WM_IPC,EVERYTHING_IPC_IS_QUICK_LAUNCH_SHORTCUT,0);
#define EVERYTHING_IPC_IS_DESKTOP_SHORTCUT								302 // int ret = (int)SendMessage(everything_taskbar_notification_hwnd,EVERYTHING_WM_IPC,EVERYTHING_IPC_IS_DESKTOP_SHORTCUT,0);
#define EVERYTHING_IPC_IS_FOLDER_CONTEXT_MENU							303 // int ret = (int)SendMessage(everything_taskbar_notification_hwnd,EVERYTHING_WM_IPC,EVERYTHING_IPC_IS_FOLDER_CONTEXT_MENU,0);
#define EVERYTHING_IPC_IS_RUN_ON_SYSTEM_STARTUP							304 // int ret = (int)SendMessage(everything_taskbar_notification_hwnd,EVERYTHING_WM_IPC,EVERYTHING_IPC_IS_RUN_ON_SYSTEM_STARTUP,0);
#define EVERYTHING_IPC_IS_URL_PROTOCOL									305 // int ret = (int)SendMessage(everything_taskbar_notification_hwnd,EVERYTHING_WM_IPC,EVERYTHING_IPC_IS_URL_PROTOCOL,0);
#define EVERYTHING_IPC_IS_SERVICE										306 // int ret = (int)SendMessage(everything_taskbar_notification_hwnd,EVERYTHING_WM_IPC,EVERYTHING_IPC_IS_SERVICE,0);

// indexing
#define EVERYTHING_IPC_IS_NTFS_DRIVE_INDEXED							400 // int is_indexed = (int)SendMessage(everything_taskbar_notification_hwnd,EVERYTHING_WM_IPC,EVERYTHING_IPC_IS_NTFS_DRIVE_INDEXED,drive_index); drive_index: 0-25 = 0=A:, 1=B:, 2=C:...

// requires Everything 1.4:
#define EVERYTHING_IPC_IS_DB_LOADED										401 // int is_db_loaded = (int)SendMessage(everything_taskbar_notification_hwnd,EVERYTHING_WM_IPC,EVERYTHING_IPC_IS_DB_LOADED,0); 
#define EVERYTHING_IPC_IS_DB_BUSY										402 // int is_db_busy = (int)SendMessage(everything_taskbar_notification_hwnd,EVERYTHING_WM_IPC,EVERYTHING_IPC_IS_DB_BUSY,0); // db is busy, issueing another action will cancel the current one (if possible).
#define EVERYTHING_IPC_IS_ADMIN											403 // int is_admin = (int)SendMessage(everything_taskbar_notification_hwnd,EVERYTHING_WM_IPC,EVERYTHING_IPC_IS_ADMIN,0);
#define EVERYTHING_IPC_IS_APPDATA										404 // int is_appdata = (int)SendMessage(everything_taskbar_notification_hwnd,EVERYTHING_WM_IPC,EVERYTHING_IPC_IS_APPDATA,0);
#define EVERYTHING_IPC_REBUILD_DB										405 // SendMessage(everything_taskbar_notification_hwnd,EVERYTHING_WM_IPC,EVERYTHING_IPC_REBUILD,0); // forces all indexes to be rescanned.
#define EVERYTHING_IPC_UPDATE_ALL_FOLDER_INDEXES						406 // SendMessage(everything_taskbar_notification_hwnd,EVERYTHING_WM_IPC,EVERYTHING_IPC_UPDATE_ALL_FOLDER_INDEXES,0); // rescan all folder indexes.
#define EVERYTHING_IPC_SAVE_DB											407 // SendMessage(everything_taskbar_notification_hwnd,EVERYTHING_WM_IPC,EVERYTHING_IPC_SAVE_DB,0); // save the db to disk.
#define EVERYTHING_IPC_SAVE_RUN_HISTORY									408 // SendMessage(everything_taskbar_notification_hwnd,EVERYTHING_WM_IPC,EVERYTHING_IPC_SAVE_RUN_HISTORY,0); // save run history to disk.
#define EVERYTHING_IPC_DELETE_RUN_HISTORY								409 // SendMessage(everything_taskbar_notification_hwnd,EVERYTHING_WM_IPC,EVERYTHING_IPC_DELETE_RUN_HISTORY,0); // deletes all run history from memory and disk.
#define EVERYTHING_IPC_IS_FAST_SORT										410 // SendMessage(everything_taskbar_notification_hwnd,EVERYTHING_WM_IPC,EVERYTHING_IPC_IS_FAST_SORT,EVERYTHING_IPC_SORT_*); // is the sort information indexed?
#define EVERYTHING_IPC_IS_FILE_INFO_INDEXED								411 // SendMessage(everything_taskbar_notification_hwnd,EVERYTHING_WM_IPC,EVERYTHING_IPC_IS_FILE_INFO_INDEXED,EVERYTHING_IPC_FILE_INFO_*); // is the file/folder info indexed?

// Everything 1.5
#define EVERYTHING_IPC_QUEUE_REBUILD_DB									412 // SendMessage(everything_taskbar_notification_hwnd,EVERYTHING_WM_IPC,EVERYTHING_IPC_QUEUE_REBUILD_DB,0); // forces all indexes to be rescanned when the db is ready.

// send the following to an existing Everything search window (requires Everything 1.4.1)
// SendMessage(FindWindow(EVERYTHING_IPC_SEARCH_CLIENT_WNDCLASS,0),EVERYTHING_WM_IPC,EVERYTHING_IPC_*,0);
#define EVERYTHING_IPC_IS_MATCH_CASE									500 // int is_match_case = (int)SendMessage(FindWindow(EVERYTHING_IPC_SEARCH_CLIENT_WNDCLASS,0),EVERYTHING_WM_IPC,EVERYTHING_IPC_IS_MATCH_CASE,0); 
#define EVERYTHING_IPC_IS_MATCH_WHOLE_WORD								501 // int is_match_whole_words = (int)SendMessage(FindWindow(EVERYTHING_IPC_SEARCH_CLIENT_WNDCLASS,0),EVERYTHING_WM_IPC,EVERYTHING_IPC_IS_MATCH_WHOLE_WORD,0); 
#define EVERYTHING_IPC_IS_MATCH_PATH									502 // int is_match_path = (int)SendMessage(FindWindow(EVERYTHING_IPC_SEARCH_CLIENT_WNDCLASS,0),EVERYTHING_WM_IPC,EVERYTHING_IPC_IS_MATCH_PATH,0); 
#define EVERYTHING_IPC_IS_MATCH_DIACRITICS								503 // int is_match_diacritics = (int)SendMessage(FindWindow(EVERYTHING_IPC_SEARCH_CLIENT_WNDCLASS,0),EVERYTHING_WM_IPC,EVERYTHING_IPC_IS_MATCH_DIACRITICS,0); 
#define EVERYTHING_IPC_IS_REGEX											504 // int is_regex = (int)SendMessage(FindWindow(EVERYTHING_IPC_SEARCH_CLIENT_WNDCLASS,0),EVERYTHING_WM_IPC,EVERYTHING_IPC_IS_REGEX,0); 
#define EVERYTHING_IPC_IS_FILTERS										505 // int is_filters = (int)SendMessage(FindWindow(EVERYTHING_IPC_SEARCH_CLIENT_WNDCLASS,0),EVERYTHING_WM_IPC,EVERYTHING_IPC_IS_FILTERS,0); 
#define EVERYTHING_IPC_IS_PREVIEW										506 // int is_preview = (int)SendMessage(FindWindow(EVERYTHING_IPC_SEARCH_CLIENT_WNDCLASS,0),EVERYTHING_WM_IPC,EVERYTHING_IPC_IS_PREVIEW,0); 
#define EVERYTHING_IPC_IS_STATUS_BAR									507 // int is_status_bar = (int)SendMessage(FindWindow(EVERYTHING_IPC_SEARCH_CLIENT_WNDCLASS,0),EVERYTHING_WM_IPC,EVERYTHING_IPC_IS_STATUS_BAR,0); 
#define EVERYTHING_IPC_IS_DETAILS										508 // int is_details = (int)SendMessage(FindWindow(EVERYTHING_IPC_SEARCH_CLIENT_WNDCLASS,0),EVERYTHING_WM_IPC,EVERYTHING_IPC_IS_DETAILS,0); 
#define EVERYTHING_IPC_GET_THUMBNAIL_SIZE								509 // int thumbnail_size = (int)SendMessage(FindWindow(EVERYTHING_IPC_SEARCH_CLIENT_WNDCLASS,0),EVERYTHING_WM_IPC,EVERYTHING_IPC_IS_GET_THUMBNAIL_SIZE,0); 0 = details
#define EVERYTHING_IPC_GET_SORT											510 // int sort = (int)SendMessage(FindWindow(EVERYTHING_IPC_SEARCH_CLIENT_WNDCLASS,0),EVERYTHING_WM_IPC,EVERYTHING_IPC_IS_GET_SORT,0); sort can be one of EVERYTHING_IPC_SORT_* types.
#define EVERYTHING_IPC_GET_ON_TOP										511 // int on_top = (int)SendMessage(FindWindow(EVERYTHING_IPC_SEARCH_CLIENT_WNDCLASS,0),EVERYTHING_WM_IPC,EVERYTHING_IPC_GET_ON_TOP,0); 0=never, 1=always, 2=while searching.
#define EVERYTHING_IPC_GET_FILTER										512 // int filter = (int)SendMessage(FindWindow(EVERYTHING_IPC_SEARCH_CLIENT_WNDCLASS,0),EVERYTHING_WM_IPC,EVERYTHING_IPC_GET_FILTER,0); filter can be one of EVERYTHING_IPC_FILTER_* types.
#define EVERYTHING_IPC_GET_FILTER_INDEX									513 // int filter_index = (int)SendMessage(FindWindow(EVERYTHING_IPC_SEARCH_CLIENT_WNDCLASS,0),EVERYTHING_WM_IPC,EVERYTHING_IPC_GET_FILTER_INDEX,0); 

// Everything 1.5
#define EVERYTHING_IPC_IS_MATCH_PREFIX									514	// int is_match_prefix = (int)SendMessage(FindWindow(EVERYTHING_IPC_SEARCH_CLIENT_WNDCLASS,0),EVERYTHING_WM_IPC,EVERYTHING_IPC_IS_MATCH_PREFIX,0); 
#define EVERYTHING_IPC_IS_MATCH_SUFFIX									515	// int is_match_suffix = (int)SendMessage(FindWindow(EVERYTHING_IPC_SEARCH_CLIENT_WNDCLASS,0),EVERYTHING_WM_IPC,EVERYTHING_IPC_IS_MATCH_SUFFIX,0); 
#define EVERYTHING_IPC_IS_IGNORE_PUNCTUATION							516	// int is_ignore_punctuation = (int)SendMessage(FindWindow(EVERYTHING_IPC_SEARCH_CLIENT_WNDCLASS,0),EVERYTHING_WM_IPC,EVERYTHING_IPC_IS_IGNORE_PUNCTUATION,0); 
#define EVERYTHING_IPC_IS_IGNORE_WHITESPACE								517	// int is_ignore_whitespace = (int)SendMessage(FindWindow(EVERYTHING_IPC_SEARCH_CLIENT_WNDCLASS,0),EVERYTHING_WM_IPC,EVERYTHING_IPC_IS_IGNORE_WHITESPACE,0); 
#define EVERYTHING_IPC_IS_SEARCH_AS_YOU_TYPE							518 // int is_search_as_you_type = (int)SendMessage(FindWindow(EVERYTHING_IPC_SEARCH_CLIENT_WNDCLASS,0),EVERYTHING_WM_IPC,EVERYTHING_IPC_IS_SEARCH_AS_YOU_TYPE,0); 

// command IDs to send to an Everything search window.
// SendMessage(FindWindow(EVERYTHING_IPC_SEARCH_CLIENT_WNDCLASS,0),WM_COMMAND,MAKEWPARAM(EVERYTHING_IPC_ID_*,0),0);

// main menus

#define	EVERYTHING_IPC_ID_FILE_MENU										10001
#define	EVERYTHING_IPC_ID_EDIT_MENU										10002
#define	EVERYTHING_IPC_ID_SEARCH_MENU									10003
#define	EVERYTHING_IPC_ID_TOOLS_MENU									10004
#define	EVERYTHING_IPC_ID_HELP_MENU										10005
#define	EVERYTHING_IPC_ID_TOOLBAR										10006
#define	EVERYTHING_IPC_ID_SEARCH_EDIT									10007
#define	EVERYTHING_IPC_ID_FILTER										10008
#define	EVERYTHING_IPC_ID_RESULTS_HEADER								10009
#define	EVERYTHING_IPC_ID_STATUS										10010
#define EVERYTHING_IPC_ID_VIEW_ZOOM_MENU								10012
#define	EVERYTHING_IPC_ID_VIEW_MENU										10013
#define EVERYTHING_IPC_ID_VIEW_WINDOW_SIZE_MENU							10019
#define EVERYTHING_IPC_ID_RESULT_LIST									10020
#define EVERYTHING_IPC_ID_BOOKMARKS_MENU								10021
#define EVERYTHING_IPC_ID_VIEW_SORT_BY_MENU								10022
#define EVERYTHING_IPC_ID_VIEW_GOTO_MENU								10024
#define EVERYTHING_IPC_ID_VIEW_ONTOP_MENU								10025
#define EVERYTHING_IPC_ID_PREVIEW										10026

// TRAY 
#define EVERYTHING_IPC_ID_TRAY_NEW_SEARCH_WINDOW						40001
#define EVERYTHING_IPC_ID_TRAY_CONNECT_TO_ETP_SERVER					40004
#define EVERYTHING_IPC_ID_TRAY_OPTIONS									40005
#define EVERYTHING_IPC_ID_TRAY_EXIT										40006
#define EVERYTHING_IPC_ID_TRAY_SHOW_SEARCH_WINDOW						40007
#define EVERYTHING_IPC_ID_TRAY_TOGGLE_SEARCH_WINDOW						40008

// FILE
#define EVERYTHING_IPC_ID_FILE_NEW_WINDOW								40010 
#define EVERYTHING_IPC_ID_FILE_CLOSE									40011 
#define EVERYTHING_IPC_ID_FILE_EXPORT									40012 
#define EVERYTHING_IPC_ID_FILE_EXIT										40013
#define EVERYTHING_IPC_ID_FILE_OPEN_FILELIST							40014
#define EVERYTHING_IPC_ID_FILE_CLOSE_FILELIST							40015

// EDIT
#define EVERYTHING_IPC_ID_EDIT_CUT										40020 
#define EVERYTHING_IPC_ID_EDIT_COPY										40021
#define EVERYTHING_IPC_ID_EDIT_PASTE									40022
#define EVERYTHING_IPC_ID_EDIT_SELECT_ALL								40023
#define EVERYTHING_IPC_ID_EDIT_INVERT_SELECTION							40029

// VIEW
#define EVERYTHING_IPC_ID_VIEW_ZOOM_IN									40030
#define EVERYTHING_IPC_ID_VIEW_ZOOM_OUT									40031
#define EVERYTHING_IPC_ID_VIEW_ZOOM_RESET								40032
#define EVERYTHING_IPC_ID_VIEW_TOGGLE_FULLSCREEN						40034
#define EVERYTHING_IPC_ID_VIEW_AUTO_FIT									40044
#define EVERYTHING_IPC_ID_VIEW_AUTO_SIZE_1								40045
#define EVERYTHING_IPC_ID_VIEW_AUTO_SIZE_2								40046
#define EVERYTHING_IPC_ID_VIEW_AUTO_SIZE_3								40047
#define EVERYTHING_IPC_ID_VIEW_REFRESH									40036
#define EVERYTHING_IPC_ID_VIEW_FILTERS									40035
#define EVERYTHING_IPC_ID_VIEW_SORT_BY_ASCENDING						40037
#define EVERYTHING_IPC_ID_VIEW_SORT_BY_DESCENDING						40038
#define EVERYTHING_IPC_ID_VIEW_STATUS_BAR								40039
#define EVERYTHING_IPC_ID_VIEW_GOTO_BACK								40040
#define EVERYTHING_IPC_ID_VIEW_GOTO_FORWARD								40041
#define EVERYTHING_IPC_ID_VIEW_ONTOP_NEVER								40042
#define EVERYTHING_IPC_ID_VIEW_ONTOP_ALWAYS								40043
#define EVERYTHING_IPC_ID_VIEW_ONTOP_WHILE_SEARCHING					40048
#define EVERYTHING_IPC_ID_VIEW_GOTO_HOME								40049
#define EVERYTHING_IPC_ID_VIEW_TOGGLE_LTR_RTL							40050
#define EVERYTHING_IPC_ID_VIEW_DETAILS									40051
#define EVERYTHING_IPC_ID_VIEW_MEDIUM_ICONS								40052
#define EVERYTHING_IPC_ID_VIEW_LARGE_ICONS								40053
#define EVERYTHING_IPC_ID_VIEW_EXTRA_LARGE_ICONS						40054
#define EVERYTHING_IPC_ID_VIEW_PREVIEW									40055
#define EVERYTHING_IPC_ID_VIEW_GOTO_SHOW_ALL_HISTORY					40056
#define EVERYTHING_IPC_ID_VIEW_INCREASE_THUMBNAIL_SIZE					40057
#define EVERYTHING_IPC_ID_VIEW_DECREASE_THUMBNAIL_SIZE					40058
#define EVERYTHING_IPC_ID_VIEW_SHOW_FILTERS								40096 // Everything 1.4.1
#define EVERYTHING_IPC_ID_VIEW_HIDE_FILTERS								40097 // Everything 1.4.1
#define EVERYTHING_IPC_ID_VIEW_SHOW_PREVIEW								40098 // Everything 1.4.1
#define EVERYTHING_IPC_ID_VIEW_HIDE_PREVIEW								40099 // Everything 1.4.1
#define EVERYTHING_IPC_ID_VIEW_SHOW_STATUS_BAR							40100 // Everything 1.4.1
#define EVERYTHING_IPC_ID_VIEW_HIDE_STATUS_BAR							40101 // Everything 1.4.1
#define EVERYTHING_IPC_ID_VIEW_DETAILS_NO_TOGGLE						40102 // Everything 1.4.1
#define EVERYTHING_IPC_ID_VIEW_MEDIUM_ICONS_NO_TOGGLE					40103 // Everything 1.4.1
#define EVERYTHING_IPC_ID_VIEW_LARGE_ICONS_NO_TOGGLE					40104 // Everything 1.4.1
#define EVERYTHING_IPC_ID_VIEW_EXTRA_LARGE_ICONS_NO_TOGGLE				40105 // Everything 1.4.1

// SEARCH
#define	EVERYTHING_IPC_ID_SEARCH_TOGGLE_MATCH_CASE						40060
#define EVERYTHING_IPC_ID_SEARCH_TOGGLE_MATCH_WHOLE_WORD				40061
#define EVERYTHING_IPC_ID_SEARCH_TOGGLE_MATCH_PATH						40062
#define EVERYTHING_IPC_ID_SEARCH_TOGGLE_REGEX							40063
#define	EVERYTHING_IPC_ID_SEARCH_TOGGLE_MATCH_DIACRITICS				40066
#define EVERYTHING_IPC_ID_SEARCH_FILTER_ADD								40067
#define EVERYTHING_IPC_ID_SEARCH_FILTER_ORGANIZE						40068
#define EVERYTHING_IPC_ID_SEARCH_ADVANCED_SEARCH						40069
#define	EVERYTHING_IPC_ID_SEARCH_ENABLE_MATCH_CASE						40106 // Everything 1.4.1
#define EVERYTHING_IPC_ID_SEARCH_ENABLE_MATCH_WHOLE_WORD				40107 // Everything 1.4.1
#define EVERYTHING_IPC_ID_SEARCH_ENABLE_MATCH_PATH						40108 // Everything 1.4.1
#define EVERYTHING_IPC_ID_SEARCH_ENABLE_REGEX							40109 // Everything 1.4.1
#define	EVERYTHING_IPC_ID_SEARCH_ENABLE_MATCH_DIACRITICS				40110 // Everything 1.4.1
#define	EVERYTHING_IPC_ID_SEARCH_DISABLE_MATCH_CASE						40111 // Everything 1.4.1
#define EVERYTHING_IPC_ID_SEARCH_DISABLE_MATCH_WHOLE_WORD				40112 // Everything 1.4.1
#define EVERYTHING_IPC_ID_SEARCH_DISABLE_MATCH_PATH						40113 // Everything 1.4.1
#define EVERYTHING_IPC_ID_SEARCH_DISABLE_REGEX							40114 // Everything 1.4.1
#define	EVERYTHING_IPC_ID_SEARCH_DISABLE_MATCH_DIACRITICS				40115 // Everything 1.4.1
#define EVERYTHING_IPC_ID_SEARCH_FILTER_EVERYTHING						40116 // Everything 1.4.1
#define EVERYTHING_IPC_ID_SEARCH_FILTER_AUDIO							40117 // Everything 1.4.1
#define EVERYTHING_IPC_ID_SEARCH_FILTER_COMPRESSED						40118 // Everything 1.4.1
#define EVERYTHING_IPC_ID_SEARCH_FILTER_DOCUMENT						40119 // Everything 1.4.1
#define EVERYTHING_IPC_ID_SEARCH_FILTER_EXECUTABLE						40120 // Everything 1.4.1
#define EVERYTHING_IPC_ID_SEARCH_FILTER_FOLDER							40121 // Everything 1.4.1
#define EVERYTHING_IPC_ID_SEARCH_FILTER_PICTURE							40122 // Everything 1.4.1
#define EVERYTHING_IPC_ID_SEARCH_FILTER_VIDEO							40123 // Everything 1.4.1
#define EVERYTHING_IPC_ID_SEARCH_FILTER_AUDIO_NO_TOGGLE					40124 // Everything 1.4.1
#define EVERYTHING_IPC_ID_SEARCH_FILTER_COMPRESSED_NO_TOGGLE			40125 // Everything 1.4.1
#define EVERYTHING_IPC_ID_SEARCH_FILTER_DOCUMENT_NO_TOGGLE				40126 // Everything 1.4.1
#define EVERYTHING_IPC_ID_SEARCH_FILTER_EXECUTABLE_NO_TOGGLE			40127 // Everything 1.4.1
#define EVERYTHING_IPC_ID_SEARCH_FILTER_FOLDER_NO_TOGGLE				40128 // Everything 1.4.1
#define EVERYTHING_IPC_ID_SEARCH_FILTER_PICTURE_NO_TOGGLE				40129 // Everything 1.4.1
#define EVERYTHING_IPC_ID_SEARCH_FILTER_VIDEO_NO_TOGGLE					40130 // Everything 1.4.1

// TOOLS
#define EVERYTHING_IPC_ID_TOOLS_CONNECT_TO_ETP_SERVER					40072
#define EVERYTHING_IPC_ID_TOOLS_DISCONNECT_FROM_ETP_SERVER				40073
#define EVERYTHING_IPC_ID_TOOLS_OPTIONS									40074
#define EVERYTHING_IPC_ID_TOOLS_CONSOLE									40075
#define EVERYTHING_IPC_ID_TOOLS_EDITOR									40076

// HELP
#define EVERYTHING_IPC_ID_HELP_VIEW_HELP_TOPICS							40080
#define EVERYTHING_IPC_ID_HELP_OPEN_EVERYTHING_WEBSITE					40081
#define EVERYTHING_IPC_ID_HELP_CHECK_FOR_UPDATES						40082
#define EVERYTHING_IPC_ID_HELP_ABOUT_EVERYTHING							40083
#define EVERYTHING_IPC_ID_HELP_SEARCH_SYNTAX							40084
#define EVERYTHING_IPC_ID_HELP_COMMAND_LINE_OPTIONS						40085
#define EVERYTHING_IPC_ID_HELP_REGEX_SYNTAX								40086
#define EVERYTHING_IPC_ID_HELP_DONATE									40087

// bookmarks
#define EVERYTHING_IPC_ID_BOOKMARK_ADD									40090
#define EVERYTHING_IPC_ID_BOOKMARK_ORGANIZE								40091
#define EVERYTHING_IPC_ID_BOOKMARK_START								44000
#define EVERYTHING_IPC_ID_BOOKMARK_END									45000 // exclusive

#define EVERYTHING_IPC_ID_FILTER_START									45000
#define EVERYTHING_IPC_ID_FILTER_END									46000 // exclusive

#define EVERYTHING_IPC_ID_VIEW_GOTO_START								46000
#define EVERYTHING_IPC_ID_VIEW_GOTO_END									47000 // exclusive

// files
#define EVERYTHING_IPC_ID_FILE_OPEN										41000
#define EVERYTHING_IPC_ID_FILE_OPEN_NEW									41048
#define EVERYTHING_IPC_ID_FILE_OPEN_WITH								41049
#define EVERYTHING_IPC_ID_FILE_EDIT										41050
#define EVERYTHING_IPC_ID_FILE_PLAY										41051
#define EVERYTHING_IPC_ID_FILE_PRINT									41052
#define EVERYTHING_IPC_ID_FILE_PREVIEW									41053
#define EVERYTHING_IPC_ID_FILE_PRINT_TO									41054
#define EVERYTHING_IPC_ID_FILE_RUN_AS									41055
#define EVERYTHING_IPC_ID_FILE_OPEN_WITH_DEFAULT_VERB					41056
#define EVERYTHING_IPC_ID_FILE_OPEN_AND_CLOSE							41057
#define EVERYTHING_IPC_ID_FILE_EXPLORE_PATH								41002
#define EVERYTHING_IPC_ID_FILE_OPEN_PATH								41003
#define EVERYTHING_IPC_ID_FILE_DELETE									41004
#define EVERYTHING_IPC_ID_FILE_PERMANENTLY_DELETE						41005
#define EVERYTHING_IPC_ID_FILE_RENAME									41006
#define EVERYTHING_IPC_ID_FILE_COPY_FULL_PATH_AND_NAME					41007
#define EVERYTHING_IPC_ID_FILE_COPY_PATH								41008
#define EVERYTHING_IPC_ID_FILE_PROPERTIES								41009
#define EVERYTHING_IPC_ID_FILE_READ_EXTENDED_INFORMATION				41064
#define EVERYTHING_IPC_ID_FILE_CREATE_SHORTCUT							41065
#define EVERYTHING_IPC_ID_FILE_SET_RUN_COUNT							41068
#define EVERYTHING_IPC_ID_FILE_COPY_NAME								41011
#define EVERYTHING_IPC_ID_FILE_OPEN_AND_DO_NOT_CLOSE					41076

// result list
#define EVERYTHING_IPC_ID_RESULT_LIST_EXPLORE							41001
#define EVERYTHING_IPC_ID_RESULT_LIST_FOCUS								41010
#define EVERYTHING_IPC_ID_RESULT_LIST_AUTOFIT_COLUMNS					41012
#define EVERYTHING_IPC_ID_RESULT_LIST_DOWN								41018
#define EVERYTHING_IPC_ID_RESULT_LIST_UP								41019
#define EVERYTHING_IPC_ID_RESULT_LIST_PAGE_UP							41020
#define EVERYTHING_IPC_ID_RESULT_LIST_PAGE_DOWN							41021
#define EVERYTHING_IPC_ID_RESULT_LIST_START								41022
#define EVERYTHING_IPC_ID_RESULT_LIST_END								41023
#define EVERYTHING_IPC_ID_RESULT_LIST_DOWN_EXTEND						41024
#define EVERYTHING_IPC_ID_RESULT_LIST_UP_EXTEND							41025
#define EVERYTHING_IPC_ID_RESULT_LIST_PAGE_UP_EXTEND					41026
#define EVERYTHING_IPC_ID_RESULT_LIST_PAGE_DOWN_EXTEND					41027
#define EVERYTHING_IPC_ID_RESULT_LIST_START_EXTEND						41028
#define EVERYTHING_IPC_ID_RESULT_LIST_END_EXTEND						41029
#define EVERYTHING_IPC_ID_RESULT_LIST_FOCUS_DOWN						41030
#define EVERYTHING_IPC_ID_RESULT_LIST_FOCUS_UP							41031
#define EVERYTHING_IPC_ID_RESULT_LIST_FOCUS_PAGE_UP						41032
#define EVERYTHING_IPC_ID_RESULT_LIST_FOCUS_PAGE_DOWN					41033
#define EVERYTHING_IPC_ID_RESULT_LIST_FOCUS_START						41034
#define EVERYTHING_IPC_ID_RESULT_LIST_FOCUS_END							41035
#define EVERYTHING_IPC_ID_RESULT_LIST_SCROLL_LEFT						41036
#define EVERYTHING_IPC_ID_RESULT_LIST_SCROLL_RIGHT						41037
#define EVERYTHING_IPC_ID_RESULT_LIST_SCROLL_PAGE_LEFT					41038
#define EVERYTHING_IPC_ID_RESULT_LIST_SCROLL_PAGE_RIGHT					41039
#define EVERYTHING_IPC_ID_RESULT_LIST_SELECT_FOCUS						41040
#define EVERYTHING_IPC_ID_RESULT_LIST_TOGGLE_FOCUS_SELECTION			41041
#define EVERYTHING_IPC_ID_RESULT_LIST_CONTEXT_MENU						41046
#define EVERYTHING_IPC_ID_RESULT_LIST_FOCUS_DOWN_EXTEND					41058
#define EVERYTHING_IPC_ID_RESULT_LIST_FOCUS_UP_EXTEND					41059
#define EVERYTHING_IPC_ID_RESULT_LIST_FOCUS_PAGE_UP_EXTEND				41060
#define EVERYTHING_IPC_ID_RESULT_LIST_FOCUS_PAGE_DOWN_EXTEND			41061
#define EVERYTHING_IPC_ID_RESULT_LIST_FOCUS_START_EXTEND				41062
#define EVERYTHING_IPC_ID_RESULT_LIST_FOCUS_END_EXTEND					41063
#define EVERYTHING_IPC_ID_RESULT_LIST_AUTOFIT							41066
#define EVERYTHING_IPC_ID_RESULT_LIST_COPY_CSV							41067
#define EVERYTHING_IPC_ID_RESULT_LIST_LEFT_EXTEND						41070
#define EVERYTHING_IPC_ID_RESULT_LIST_RIGHT_EXTEND						41071
#define EVERYTHING_IPC_ID_RESULT_LIST_FOCUS_LEFT_EXTEND					41072
#define EVERYTHING_IPC_ID_RESULT_LIST_FOCUS_RIGHT_EXTEND				41073
#define EVERYTHING_IPC_ID_RESULT_LIST_FOCUS_MOST_RUN					41074
#define EVERYTHING_IPC_ID_RESULT_LIST_FOCUS_LAST_RUN					41075
#define EVERYTHING_IPC_ID_RESULT_LIST_LEFT								41079 // Everything 1.4.1
#define EVERYTHING_IPC_ID_RESULT_LIST_RIGHT								41080 // Everything 1.4.1
#define EVERYTHING_IPC_ID_RESULT_LIST_FOCUS_LEFT						41081 // Everything 1.4.1
#define EVERYTHING_IPC_ID_RESULT_LIST_FOCUS_RIGHT						41082 // Everything 1.4.1
#define EVERYTHING_IPC_ID_RESULT_LIST_SCROLL_LEFT_SCROLL_ONLY			41083 // Everything 1.4.1
#define EVERYTHING_IPC_ID_RESULT_LIST_SCROLL_RIGHT_SCROLL_ONLY			41084 // Everything 1.4.1
#define EVERYTHING_IPC_ID_RESULT_LIST_SCROLL_PAGE_LEFT_SCROLL_ONLY		41085 // Everything 1.4.1
#define EVERYTHING_IPC_ID_RESULT_LIST_SCROLL_PAGE_RIGHT_SCROLL_ONLY		41086 // Everything 1.4.1

#define EVERYTHING_IPC_ID_RESULT_LIST_SORT_BY_NAME						41300
#define EVERYTHING_IPC_ID_RESULT_LIST_SORT_BY_PATH						41301
#define EVERYTHING_IPC_ID_RESULT_LIST_SORT_BY_SIZE						41302
#define EVERYTHING_IPC_ID_RESULT_LIST_SORT_BY_EXTENSION					41303
#define EVERYTHING_IPC_ID_RESULT_LIST_SORT_BY_TYPE						41304
#define EVERYTHING_IPC_ID_RESULT_LIST_SORT_BY_DATE_MODIFIED				41305
#define EVERYTHING_IPC_ID_RESULT_LIST_SORT_BY_DATE_CREATED				41306
#define EVERYTHING_IPC_ID_RESULT_LIST_SORT_BY_ATTRIBUTES				41307
#define EVERYTHING_IPC_ID_RESULT_LIST_SORT_BY_FILE_LIST_FILENAME		41308
#define EVERYTHING_IPC_ID_RESULT_LIST_SORT_BY_RUN_COUNT					41309
#define EVERYTHING_IPC_ID_RESULT_LIST_SORT_BY_DATE_RECENTLY_CHANGED		41310
#define EVERYTHING_IPC_ID_RESULT_LIST_SORT_BY_DATE_ACCESSED				41311
#define EVERYTHING_IPC_ID_RESULT_LIST_SORT_BY_DATE_RUN					41312

#define EVERYTHING_IPC_ID_RESULT_LIST_TOGGLE_NAME_COLUMN					41400
#define EVERYTHING_IPC_ID_RESULT_LIST_TOGGLE_PATH_COLUMN					41401
#define EVERYTHING_IPC_ID_RESULT_LIST_TOGGLE_SIZE_COLUMN					41402
#define EVERYTHING_IPC_ID_RESULT_LIST_TOGGLE_EXTENSION_COLUMN				41403
#define EVERYTHING_IPC_ID_RESULT_LIST_TOGGLE_TYPE_COLUMN					41404
#define EVERYTHING_IPC_ID_RESULT_LIST_TOGGLE_DATE_MODIFIED_COLUMN			41405
#define EVERYTHING_IPC_ID_RESULT_LIST_TOGGLE_DATE_CREATED_COLUMN			41406
#define EVERYTHING_IPC_ID_RESULT_LIST_TOGGLE_ATTRIBUTES_COLUMN				41407
#define EVERYTHING_IPC_ID_RESULT_LIST_TOGGLE_FILE_LIST_FILENAME_COLUMN		41408
#define EVERYTHING_IPC_ID_RESULT_LIST_TOGGLE_RUN_COUNT_COLUMN				41409
#define EVERYTHING_IPC_ID_RESULT_LIST_TOGGLE_DATE_RECENTLY_CHANGED_COLUMN	41410
#define EVERYTHING_IPC_ID_RESULT_LIST_TOGGLE_DATE_ACCESSED_COLUMN			41411
#define EVERYTHING_IPC_ID_RESULT_LIST_TOGGLE_DATE_RUN_COLUMN				41412

#define EVERYTHING_IPC_ID_RESULT_LIST_SIZE_NAME_COLUMN_TO_FIT					41600
#define EVERYTHING_IPC_ID_RESULT_LIST_SIZE_PATH_COLUMN_TO_FIT					41601
#define EVERYTHING_IPC_ID_RESULT_LIST_SIZE_SIZE_COLUMN_TO_FIT					41602
#define EVERYTHING_IPC_ID_RESULT_LIST_SIZE_EXTENSION_COLUMN_TO_FIT				41603
#define EVERYTHING_IPC_ID_RESULT_LIST_SIZE_TYPE_COLUMN_TO_FIT					41604
#define EVERYTHING_IPC_ID_RESULT_LIST_SIZE_DATE_MODIFIED_COLUMN_TO_FIT			41605
#define EVERYTHING_IPC_ID_RESULT_LIST_SIZE_DATE_CREATED_COLUMN_TO_FIT			41606
#define EVERYTHING_IPC_ID_RESULT_LIST_SIZE_ATTRIBUTES_COLUMN_TO_FIT				41607
#define EVERYTHING_IPC_ID_RESULT_LIST_SIZE_FILE_LIST_FILENAME_COLUMN_TO_FIT		41608
#define EVERYTHING_IPC_ID_RESULT_LIST_SIZE_RUN_COUNT_COLUMN_TO_FIT				41609
#define EVERYTHING_IPC_ID_RESULT_LIST_SIZE_DATE_RECENTLY_CHANGED_COLUMN_TO_FIT	41610
#define EVERYTHING_IPC_ID_RESULT_LIST_SIZE_DATE_ACCESSED_COLUMN_TO_FIT			41611
#define EVERYTHING_IPC_ID_RESULT_LIST_SIZE_DATE_RUN_COLUMN_TO_FIT				41612

#define EVERYTHING_IPC_ID_FILE_CUSTOM_VERB01							41500
#define EVERYTHING_IPC_ID_FILE_CUSTOM_VERB02							41501
#define EVERYTHING_IPC_ID_FILE_CUSTOM_VERB03							41502
#define EVERYTHING_IPC_ID_FILE_CUSTOM_VERB04							41503
#define EVERYTHING_IPC_ID_FILE_CUSTOM_VERB05							41504
#define EVERYTHING_IPC_ID_FILE_CUSTOM_VERB06							41505
#define EVERYTHING_IPC_ID_FILE_CUSTOM_VERB07							41506
#define EVERYTHING_IPC_ID_FILE_CUSTOM_VERB08							41507
#define EVERYTHING_IPC_ID_FILE_CUSTOM_VERB09							41508
#define EVERYTHING_IPC_ID_FILE_CUSTOM_VERB10							41509
#define EVERYTHING_IPC_ID_FILE_CUSTOM_VERB11							41510
#define EVERYTHING_IPC_ID_FILE_CUSTOM_VERB12							41511

// search
#define EVERYTHING_IPC_ID_SEARCH_EDIT_FOCUS								42000
#define EVERYTHING_IPC_ID_SEARCH_EDIT_WORD_DELETE_TO_START				42019
#define	EVERYTHING_IPC_ID_SEARCH_EDIT_AUTO_COMPLETE						42020
#define EVERYTHING_IPC_ID_SEARCH_EDIT_SHOW_SEARCH_HISTORY				42021
#define EVERYTHING_IPC_ID_SEARCH_EDIT_SHOW_ALL_SEARCH_HISTORY			42022

#define EVERYTHING_IPC_ID_TRAY_EDITOR									41700
#define EVERYTHING_IPC_ID_TRAY_OPEN_FILELIST							41701

#define EVERYTHING_IPC_ID_INDEX_UPDATE_ALL_FOLDERS_NOW					41800
#define EVERYTHING_IPC_ID_INDEX_FORCE_REBUILD							41801

// find the everything IPC window
#define EVERYTHING_IPC_WNDCLASSW										L"EVERYTHING_TASKBAR_NOTIFICATION"
#define EVERYTHING_IPC_WNDCLASSA										"EVERYTHING_TASKBAR_NOTIFICATION"

// an Everything search window
#define EVERYTHING_IPC_SEARCH_CLIENT_WNDCLASSW							L"EVERYTHING"
#define EVERYTHING_IPC_SEARCH_CLIENT_WNDCLASSA							"EVERYTHING"

// this global window message is sent to all top level windows when everything starts.
#define EVERYTHING_IPC_CREATEDW											L"EVERYTHING_IPC_CREATED"
#define EVERYTHING_IPC_CREATEDA											"EVERYTHING_IPC_CREATED"

// search flags for querys
#define EVERYTHING_IPC_MATCHCASE										0x00000001	// match case
#define EVERYTHING_IPC_MATCHWHOLEWORD									0x00000002	// match whole word
#define EVERYTHING_IPC_MATCHPATH										0x00000004	// include paths in search
#define EVERYTHING_IPC_REGEX											0x00000008	// enable regex
#define EVERYTHING_IPC_MATCHACCENTS										0x00000010	// match diacritic marks
#define EVERYTHING_IPC_MATCHDIACRITICS									0x00000010	// match diacritic marks
#define EVERYTHING_IPC_MATCHPREFIX										0x00000020	// match prefix (Everything 1.5)
#define EVERYTHING_IPC_MATCHSUFFIX										0x00000040	// match suffix (Everything 1.5)
#define EVERYTHING_IPC_IGNOREPUNCTUATION								0x00000080	// ignore punctuation (Everything 1.5)
#define EVERYTHING_IPC_IGNOREWHITESPACE									0x00000100	// ignore white-space (Everything 1.5)

// item flags
#define EVERYTHING_IPC_FOLDER											0x00000001	// The item is a folder. (it's a file if not set)
#define EVERYTHING_IPC_DRIVE											0x00000002	// the file or folder is a drive/root.
#define EVERYTHING_IPC_ROOT												0x00000002	// the file or folder is a root.
																					
typedef struct EVERYTHING_IPC_COMMAND_LINE
{
	DWORD show_command; // MUST be one of the SW_* ShowWindow() commands
	
	// null terminated variable sized command line text in UTF-8.
	BYTE command_line_text[1];
	
}EVERYTHING_IPC_COMMAND_LINE;

// the WM_COPYDATA message for a query.
#define EVERYTHING_IPC_COPYDATA_COMMAND_LINE_UTF8						0  // Send a EVERYTHING_IPC_COMMAND_LINE structure.
#define EVERYTHING_IPC_COPYDATAQUERYA									1
#define EVERYTHING_IPC_COPYDATAQUERYW									2

// all results
#define EVERYTHING_IPC_ALLRESULTS										0xFFFFFFFF // all results

// macro to get the filename of an item
#define EVERYTHING_IPC_ITEMFILENAMEA(list,item) (CHAR *)((CHAR *)(list) + ((EVERYTHING_IPC_ITEMA *)(item))->filename_offset)
#define EVERYTHING_IPC_ITEMFILENAMEW(list,item) (WCHAR *)((CHAR *)(list) + ((EVERYTHING_IPC_ITEMW *)(item))->filename_offset)

// macro to get the path of an item
#define EVERYTHING_IPC_ITEMPATHA(list,item) (CHAR *)((CHAR *)(list) + ((EVERYTHING_IPC_ITEMA *)(item))->path_offset)
#define EVERYTHING_IPC_ITEMPATHW(list,item) (WCHAR *)((CHAR *)(list) + ((EVERYTHING_IPC_ITEMW *)(item))->path_offset)

#pragma pack (push,1)

//
// Varible sized query struct sent to everything.
//
// sent in the form of a WM_COPYDATA message with EVERYTHING_IPC_COPYDATAQUERY as the 
// dwData member in the COPYDATASTRUCT struct.
// set the lpData member of the COPYDATASTRUCT struct to point to your EVERYTHING_IPC_QUERY struct.
// set the cbData member of the COPYDATASTRUCT struct to the size of the 
// EVERYTHING_IPC_QUERY struct minus the size of a TCHAR plus the length of the search string in bytes plus 
// one TCHAR for the null terminator.
//
// NOTE: to determine the size of this structure use 
// ASCII: sizeof(EVERYTHING_IPC_QUERYA) - sizeof(CHAR) + strlen(search_string)*sizeof(CHAR) + sizeof(CHAR)
// UNICODE: sizeof(EVERYTHING_IPC_QUERYW) - sizeof(WCHAR) + wcslen(search_string)*sizeof(WCHAR) + sizeof(WCHAR)
//
// NOTE: Everything will only do one query per window.
// Sending another query when a query has not completed 
// will cancel the old query and start the new one. 
//
// Everything will send the results to the reply_hwnd in the form of a 
// WM_COPYDATA message with the dwData value you specify.
// 
// Everything will return TRUE if successful.
// returns FALSE if not supported.
//
// If you query with EVERYTHING_IPC_COPYDATAQUERYW, the results sent from Everything will be Unicode.
//

typedef struct EVERYTHING_IPC_QUERYW
{
	// the window that will receive the new results.
	// only 32bits are required to store a window handle. (even on x64)
	DWORD reply_hwnd;
	
	// the value to set the dwData member in the COPYDATASTRUCT struct 
	// sent by Everything when the query is complete.
	DWORD reply_copydata_message;
	
	// search flags (see EVERYTHING_IPC_MATCHCASE | EVERYTHING_IPC_MATCHWHOLEWORD | EVERYTHING_IPC_MATCHPATH)
	DWORD search_flags; 
	
	// only return results after 'offset' results (0 to return from the first result)
	// useful for scrollable lists
	DWORD offset; 
	
	// the number of results to return 
	// zero to return no results
	// EVERYTHING_IPC_ALLRESULTS to return ALL results
	DWORD max_results;

	// null terminated string. variable lengthed search string buffer.
	WCHAR search_string[1];
	
}EVERYTHING_IPC_QUERYW;

// ASCII version
typedef struct EVERYTHING_IPC_QUERYA
{
	// the window that will receive the new results.
	// only 32bits are required to store a window handle. (even on x64)
	DWORD reply_hwnd;
	
	// the value to set the dwData member in the COPYDATASTRUCT struct 
	// sent by Everything when the query is complete.
	DWORD reply_copydata_message;
	
	// search flags (see EVERYTHING_IPC_MATCHCASE | EVERYTHING_IPC_MATCHWHOLEWORD | EVERYTHING_IPC_MATCHPATH)
	DWORD search_flags; 
	
	// only return results after 'offset' results (0 to return from the first result)
	// useful for scrollable lists
	DWORD offset; 
	
	// the number of results to return 
	// zero to return no results
	// EVERYTHING_IPC_ALLRESULTS to return ALL results
	DWORD max_results;

	// null terminated string. variable lengthed search string buffer.
	CHAR search_string[1];
	
}EVERYTHING_IPC_QUERYA;

//
// Varible sized result list struct received from Everything.
//
// Sent in the form of a WM_COPYDATA message to the hwnd specifed in the 
// EVERYTHING_IPC_QUERY struct.
// the dwData member of the COPYDATASTRUCT struct will match the sent
// reply_copydata_message member in the EVERYTHING_IPC_QUERY struct.
// 
// make a copy of the data before returning.
//
// return TRUE if you processed the WM_COPYDATA message.
//

typedef struct EVERYTHING_IPC_ITEMW
{
	// item flags
	DWORD flags;

	// The offset of the filename from the beginning of the list structure.
	// (wchar_t *)((char *)everything_list + everythinglist->name_offset)
	DWORD filename_offset;

	// The offset of the filename from the beginning of the list structure.
	// (wchar_t *)((char *)everything_list + everythinglist->path_offset)
	DWORD path_offset;
	
}EVERYTHING_IPC_ITEMW;

typedef struct EVERYTHING_IPC_ITEMA
{
	// item flags
	DWORD flags;

	// The offset of the filename from the beginning of the list structure.
	// (char *)((char *)everything_list + everythinglist->name_offset)
	DWORD filename_offset;

	// The offset of the filename from the beginning of the list structure.
	// (char *)((char *)everything_list + everythinglist->path_offset)
	DWORD path_offset;
	
}EVERYTHING_IPC_ITEMA;

typedef struct EVERYTHING_IPC_LISTW
{
	// the total number of folders found.
	DWORD totfolders;
	
	// the total number of files found.
	DWORD totfiles;
	
	// totfolders + totfiles
	DWORD totitems;
	
	// the number of folders available.
	DWORD numfolders;
	
	// the number of files available.
	DWORD numfiles;
	
	// the number of items available.
	DWORD numitems;

	// index offset of the first result in the item list.
	DWORD offset;
	
	// variable lengthed item list. 
	// use numitems to determine the actual number of items available.
	EVERYTHING_IPC_ITEMW items[1];
	
}EVERYTHING_IPC_LISTW;

typedef struct EVERYTHING_IPC_LISTA
{
	// the total number of folders found.
	DWORD totfolders;
	
	// the total number of files found.
	DWORD totfiles;
	
	// totfolders + totfiles
	DWORD totitems;
	
	// the number of folders available.
	DWORD numfolders;
	
	// the number of files available.
	DWORD numfiles;
	
	// the number of items available.
	DWORD numitems;

	// index offset of the first result in the item list.
	DWORD offset;
	
	// variable lengthed item list. 
	// use numitems to determine the actual number of items available.
	EVERYTHING_IPC_ITEMA items[1];
	
}EVERYTHING_IPC_LISTA;

#pragma pack (pop)

#ifdef UNICODE
#define EVERYTHING_IPC_COPYDATAQUERY	EVERYTHING_IPC_COPYDATAQUERYW
#define EVERYTHING_IPC_ITEMFILENAME		EVERYTHING_IPC_ITEMFILENAMEW
#define EVERYTHING_IPC_ITEMPATH			EVERYTHING_IPC_ITEMPATHW
#define EVERYTHING_IPC_QUERY			EVERYTHING_IPC_QUERYW
#define EVERYTHING_IPC_ITEM				EVERYTHING_IPC_ITEMW
#define EVERYTHING_IPC_LIST				EVERYTHING_IPC_LISTW
#define EVERYTHING_IPC_WNDCLASS			EVERYTHING_IPC_WNDCLASSW
#define EVERYTHING_IPC_SEARCH_CLIENT_WNDCLASS			EVERYTHING_IPC_SEARCH_CLIENT_WNDCLASSW
#define EVERYTHING_IPC_CREATED			EVERYTHING_IPC_CREATEDW
#else
#define EVERYTHING_IPC_COPYDATAQUERY	EVERYTHING_IPC_COPYDATAQUERYA
#define EVERYTHING_IPC_ITEMFILENAME		EVERYTHING_IPC_ITEMFILENAMEA
#define EVERYTHING_IPC_ITEMPATH			EVERYTHING_IPC_ITEMPATHA
#define EVERYTHING_IPC_QUERY			EVERYTHING_IPC_QUERYA
#define EVERYTHING_IPC_ITEM				EVERYTHING_IPC_ITEMA
#define EVERYTHING_IPC_LIST				EVERYTHING_IPC_LISTA
#define EVERYTHING_IPC_WNDCLASS			EVERYTHING_IPC_WNDCLASSA
#define EVERYTHING_IPC_SEARCH_CLIENT_WNDCLASS			EVERYTHING_IPC_SEARCH_CLIENT_WNDCLASSA
#define EVERYTHING_IPC_CREATED			EVERYTHING_IPC_CREATEDA
#endif

// the WM_COPYDATA message for a query.
// requires Everything 1.4.1
#define EVERYTHING_IPC_COPYDATA_QUERY2A									17
#define EVERYTHING_IPC_COPYDATA_QUERY2W									18

#define EVERYTHING_IPC_SORT_NAME_ASCENDING								1
#define EVERYTHING_IPC_SORT_NAME_DESCENDING								2
#define EVERYTHING_IPC_SORT_PATH_ASCENDING								3
#define EVERYTHING_IPC_SORT_PATH_DESCENDING								4
#define EVERYTHING_IPC_SORT_SIZE_ASCENDING								5
#define EVERYTHING_IPC_SORT_SIZE_DESCENDING								6
#define EVERYTHING_IPC_SORT_EXTENSION_ASCENDING							7
#define EVERYTHING_IPC_SORT_EXTENSION_DESCENDING						8
#define EVERYTHING_IPC_SORT_TYPE_NAME_ASCENDING							9
#define EVERYTHING_IPC_SORT_TYPE_NAME_DESCENDING						10
#define EVERYTHING_IPC_SORT_DATE_CREATED_ASCENDING						11
#define EVERYTHING_IPC_SORT_DATE_CREATED_DESCENDING						12
#define EVERYTHING_IPC_SORT_DATE_MODIFIED_ASCENDING						13
#define EVERYTHING_IPC_SORT_DATE_MODIFIED_DESCENDING					14
#define EVERYTHING_IPC_SORT_ATTRIBUTES_ASCENDING						15
#define EVERYTHING_IPC_SORT_ATTRIBUTES_DESCENDING						16
#define EVERYTHING_IPC_SORT_FILE_LIST_FILENAME_ASCENDING				17
#define EVERYTHING_IPC_SORT_FILE_LIST_FILENAME_DESCENDING				18
#define EVERYTHING_IPC_SORT_RUN_COUNT_ASCENDING							19
#define EVERYTHING_IPC_SORT_RUN_COUNT_DESCENDING						20
#define EVERYTHING_IPC_SORT_DATE_RECENTLY_CHANGED_ASCENDING				21
#define EVERYTHING_IPC_SORT_DATE_RECENTLY_CHANGED_DESCENDING			22
#define EVERYTHING_IPC_SORT_DATE_ACCESSED_ASCENDING						23
#define EVERYTHING_IPC_SORT_DATE_ACCESSED_DESCENDING					24
#define EVERYTHING_IPC_SORT_DATE_RUN_ASCENDING							25
#define EVERYTHING_IPC_SORT_DATE_RUN_DESCENDING							26

#define EVERYTHING_IPC_QUERY2_REQUEST_NAME								0x00000001
#define EVERYTHING_IPC_QUERY2_REQUEST_PATH								0x00000002
#define EVERYTHING_IPC_QUERY2_REQUEST_FULL_PATH_AND_NAME				0x00000004
#define EVERYTHING_IPC_QUERY2_REQUEST_EXTENSION							0x00000008
#define EVERYTHING_IPC_QUERY2_REQUEST_SIZE								0x00000010
#define EVERYTHING_IPC_QUERY2_REQUEST_DATE_CREATED						0x00000020
#define EVERYTHING_IPC_QUERY2_REQUEST_DATE_MODIFIED						0x00000040
#define EVERYTHING_IPC_QUERY2_REQUEST_DATE_ACCESSED						0x00000080
#define EVERYTHING_IPC_QUERY2_REQUEST_ATTRIBUTES						0x00000100
#define EVERYTHING_IPC_QUERY2_REQUEST_FILE_LIST_FILE_NAME				0x00000200
#define EVERYTHING_IPC_QUERY2_REQUEST_RUN_COUNT							0x00000400
#define EVERYTHING_IPC_QUERY2_REQUEST_DATE_RUN							0x00000800
#define EVERYTHING_IPC_QUERY2_REQUEST_DATE_RECENTLY_CHANGED				0x00001000
#define EVERYTHING_IPC_QUERY2_REQUEST_HIGHLIGHTED_NAME					0x00002000
#define EVERYTHING_IPC_QUERY2_REQUEST_HIGHLIGHTED_PATH					0x00004000
#define EVERYTHING_IPC_QUERY2_REQUEST_HIGHLIGHTED_FULL_PATH_AND_NAME	0x00008000

#define EVERYTHING_IPC_FILE_INFO_FILE_SIZE								1
#define EVERYTHING_IPC_FILE_INFO_FOLDER_SIZE							2
#define EVERYTHING_IPC_FILE_INFO_DATE_CREATED							3
#define EVERYTHING_IPC_FILE_INFO_DATE_MODIFIED							4
#define EVERYTHING_IPC_FILE_INFO_DATE_ACCESSED							5
#define EVERYTHING_IPC_FILE_INFO_ATTRIBUTES								6

#pragma pack (push,1)

//
// Varible sized query struct sent to everything.
//
// sent in the form of a WM_COPYDATA message with EVERYTHING_IPC_COPYDATA_QUERY2 as the 
// dwData member in the COPYDATASTRUCT struct.
// set the lpData member of the COPYDATASTRUCT struct to point to your EVERYTHING_IPC_QUERY struct.
// set the cbData member of the COPYDATASTRUCT struct to the size of the 
// EVERYTHING_IPC_QUERY struct minus the size of a TCHAR plus the length of the search string in bytes plus 
// one TCHAR for the null terminator.
//
// NOTE: Everything will only do one query per window.
// Sending another query when a query has not completed 
// will cancel the old query and start the new one. 
//
// Everything will send the results to the reply_hwnd in the form of a 
// WM_COPYDATA message with the dwData value you specify.
// 
// Everything will return TRUE if successful.
// returns FALSE if not supported.
//
// If you query with EVERYTHING_IPC_COPYDATA_QUERYW, the results sent from Everything will be Unicode.
//

// ASCII version
typedef struct EVERYTHING_IPC_QUERY2
{
	// the window that will receive the new results.
	// only 32bits are required to store a window handle. (even on x64)
	DWORD reply_hwnd;
	
	// the value to set the dwData member in the COPYDATASTRUCT struct 
	// sent by Everything when the query is complete.
	DWORD reply_copydata_message;
	
	// search flags (see EVERYTHING_IPC_MATCHCASE | EVERYTHING_IPC_MATCHWHOLEWORD | EVERYTHING_IPC_MATCHPATH)
	DWORD search_flags; 
	
	// only return results after 'offset' results (0 to return from the first result)
	// useful for scrollable lists
	DWORD offset; 
	
	// the number of results to return 
	// zero to return no results
	// EVERYTHING_IPC_ALLRESULTS to return ALL results
	DWORD max_results;
	
	// request types.
	// one or more of EVERYTHING_IPC_QUERY2_REQUEST_* types.
	DWORD request_flags;

	// sort type, set to one of EVERYTHING_IPC_SORT_* types.
	// set to EVERYTHING_IPC_SORT_NAME_ASCENDING for the best performance (there will never be a performance hit when sorting by name ascending).
	// Other sorts will also be instant if the corresponding fast sort is enabled from Tools -> Options -> Indexes.
	DWORD sort_type;

	// followed by null terminated search.
	// TCHAR search_string[1];
		
}EVERYTHING_IPC_QUERY2;

typedef struct EVERYTHING_IPC_ITEM2
{
	// item flags one of (EVERYTHING_IPC_FOLDER|EVERYTHING_IPC_DRIVE|EVERYTHING_IPC_ROOT)
	DWORD flags;
	
	// offset from the start of the EVERYTHING_IPC_LIST2 struct to the data content
	DWORD data_offset;

	// data found at data_offset
	// if EVERYTHING_IPC_QUERY2_REQUEST_NAME was set in request_flags, DWORD name_length in characters (excluding the null terminator); followed by null terminated text.
	// if EVERYTHING_IPC_QUERY2_REQUEST_PATH was set in request_flags, DWORD name_length in characters (excluding the null terminator); followed by null terminated text.
	// if EVERYTHING_IPC_QUERY2_REQUEST_FULL_PATH_AND_NAME was set in request_flags, DWORD name_length (excluding the null terminator); followed by null terminated text.
	// if EVERYTHING_IPC_QUERY2_REQUEST_SIZE was set in request_flags, LARGE_INTERGER size;
	// if EVERYTHING_IPC_QUERY2_REQUEST_EXTENSION was set in request_flags, DWORD name_length in characters (excluding the null terminator); followed by null terminated text;
	// if EVERYTHING_IPC_QUERY2_REQUEST_TYPE_NAME was set in request_flags, DWORD name_length in characters (excluding the null terminator); followed by null terminated text;
	// if EVERYTHING_IPC_QUERY2_REQUEST_DATE_CREATED was set in request_flags, FILETIME date;
	// if EVERYTHING_IPC_QUERY2_REQUEST_DATE_MODIFIED was set in request_flags, FILETIME date;
	// if EVERYTHING_IPC_QUERY2_REQUEST_DATE_ACCESSED was set in request_flags, FILETIME date;
	// if EVERYTHING_IPC_QUERY2_REQUEST_ATTRIBUTES was set in request_flags, DWORD attributes;
	// if EVERYTHING_IPC_QUERY2_REQUEST_FILELIST_FILENAME was set in request_flags, DWORD name_length in characters (excluding the null terminator); followed by null terminated text;
	// if EVERYTHING_IPC_QUERY2_REQUEST_RUN_COUNT was set in request_flags, DWORD run_count;
	// if EVERYTHING_IPC_QUERY2_REQUEST_DATE_RUN was set in request_flags, FILETIME date;
	// if EVERYTHING_IPC_QUERY2_REQUEST_DATE_RECENTLY_CHANGED was set in request_flags, FILETIME date;
	// if EVERYTHING_IPC_QUERY2_REQUEST_HIGHLIGHTED_NAME was set in request_flags, DWORD name_length in characters (excluding the null terminator); followed by null terminated text; ** = *, *text* = highlighted text
	// if EVERYTHING_IPC_QUERY2_REQUEST_HIGHLIGHTED_PATH was set in request_flags, DWORD name_length in characters (excluding the null terminator); followed by null terminated text; ** = *, *text* = highlighted text
	// if EVERYTHING_IPC_QUERY2_REQUEST_HIGHLIGHTED_FULL_PATH_AND_NAME was set in request_flags, DWORD name_length in characters (excluding the null terminator); followed by null terminated text; ** = *, *text* = highlighted text
	
}EVERYTHING_IPC_ITEM2;

typedef struct EVERYTHING_IPC_LIST2
{
	// number of items found.
	DWORD totitems;
	
	// the number of items available.
	DWORD numitems;

	// index offset of the first result in the item list.
	DWORD offset;
	
	// valid request types.
	DWORD request_flags;
	
	// this sort type.
	// one of EVERYTHING_IPC_SORT_* types.
	// maybe different to requested sort type.
	DWORD sort_type;
	
	// items follow.
	// EVERYTHING_IPC_ITEM2 items[numitems]
	
	// item data follows.
	
}EVERYTHING_IPC_LIST2;

#pragma pack (pop)

// Get the Run Count for a file, by filename.
// COPYDATASTRUCT cds;
// cds.dwData = EVERYTHING_IPC_COPYDATA_GET_RUN_COUNTA;
// cds.lpData = TEXT("C:\\folder\\file.txt");
// cds.cbData = size in bytes of cds.lpData including null terminator.
// SendMessage(everything_taskbar_notification_hwnd,WM_COPYDATA,(WPARAM)(HWND)notify_hwnd,(LPARAM)(COPYDATASTRUCT *)&cds);

#define EVERYTHING_IPC_COPYDATA_GET_RUN_COUNTA							19
#define EVERYTHING_IPC_COPYDATA_GET_RUN_COUNTW							20

#pragma pack (push,1)

typedef struct EVERYTHING_IPC_RUN_HISTORY
{
	DWORD run_count;
	
	// null terminated ansi/wchar filename follows.
	// TCHAR filename[];
	
}EVERYTHING_IPC_RUN_HISTORY;

#pragma pack (pop)

// Set the Run Count by one for a file, by filename.
// COPYDATASTRUCT cds;
// cds.dwData = EVERYTHING_IPC_COPYDATA_GET_RUN_COUNTA;
// cds.lpData = (EVERYTHING_IPC_RUN_HISTORY *)run_history;
// cds.cbData = size in bytes of cds.lpData including null terminator.
// SendMessage(everything_taskbar_notification_hwnd,WM_COPYDATA,(WPARAM)(HWND)notify_hwnd,(LPARAM)(COPYDATASTRUCT *)&cds);

#define EVERYTHING_IPC_COPYDATA_SET_RUN_COUNTA							21
#define EVERYTHING_IPC_COPYDATA_SET_RUN_COUNTW							22

// Increment the Run Count by one for a file, by filename.
// COPYDATASTRUCT cds;
// cds.dwData = EVERYTHING_IPC_COPYDATA_GET_RUN_COUNTA;
// cds.lpData = TEXT("C:\\folder\\file.txt");
// cds.cbData = size in bytes of cds.lpData including null terminator.
// SendMessage(everything_taskbar_notification_hwnd,WM_COPYDATA,(WPARAM)(HWND)notify_hwnd,(LPARAM)(COPYDATASTRUCT *)&cds);

#define EVERYTHING_IPC_COPYDATA_INC_RUN_COUNTA							23
#define EVERYTHING_IPC_COPYDATA_INC_RUN_COUNTW							24

#ifdef UNICODE
#define EVERYTHING_IPC_COPYDATA_QUERY2									EVERYTHING_IPC_COPYDATA_QUERY2W
#else
#define EVERYTHING_IPC_COPYDATA_QUERY2									EVERYTHING_IPC_COPYDATA_QUERY2A
#endif

// end extern C
#ifdef __cplusplus
}
#endif

#endif // _EVERYTHING_H_

