// NeeView.Interop.NativeTest.cpp : アプリケーションのエントリ ポイントを定義します。
//

#include "stdafx.h"
#include "NeeView.Interop.h"

#include <iostream>
#include <wrl/client.h>

void HrToStr(HRESULT hr)
{
	LPVOID string;
	FormatMessage(FORMAT_MESSAGE_ALLOCATE_BUFFER |
		FORMAT_MESSAGE_FROM_SYSTEM,
		NULL,
		hr,
		MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),
		(LPTSTR)&string,
		0,
		NULL);

	if (string != NULL)
	{
		std::wcout << "HRESULT(" << std::hex << (int)hr << "): " << (LPCTSTR)string << std::endl;
		std::wcout.flush();
	}

	LocalFree(string);
}


int main()
{
	struct Com {
		Com() { CoInitialize(nullptr); }
		~Com() { CoUninitialize(); }
	} com;

	wchar_t friendlyName[2048];
	wchar_t fileExtensions[2048];

	NVFpReset();

	setlocale(LC_ALL, "Japanese");
	//std::wcout.imbue(std::locale("Japanese"));

	{
		for (int i = 0;; ++i)
		{
			try
			{
				bool result = NVGetImageCodecInfo(i, friendlyName, fileExtensions);
				if (result)
				{
					std::wcout << friendlyName << L":  " << fileExtensions << std::endl;
				}
				else
				{
					break;
				}
			}
			catch (HRESULT hr)
			{
				HrToStr(hr);
			}
		}

		NVCloseImageCodecInfo();
	}

	{
		wchar_t shortcut1[] = L"E:\\Work\\Labo\\ショートカットテスト\\1ショート2.lnk";
		wchar_t shortcut2[] = L"E:\\Work\\Labo\\ショートカットテスト\\1ショート—2.lnk";

		wchar_t fullpath1[1024];
		NVGetFullPathFromShortcut(shortcut1, fullpath1);
		std::wcout << L"shotcut1: " << fullpath1 << std::endl;

		wchar_t fullpath2[1024];
		NVGetFullPathFromShortcut(shortcut2, fullpath2);
		std::wcout << L"shotcut2: " << fullpath2 << std::endl;
	}

	system("pause");

	return 0;
}

