# FolderTidy

<p align="center">
  <img src="FolderTidy/Assets/foldertidy-icon-512.png" alt="FolderTidy icon" width="128" height="128">
</p>

[![Stars](https://img.shields.io/github/stars/kinadog/FolderTidy?style=flat-square&logo=github)](https://github.com/kinadog/FolderTidy/stargazers)
[![Forks](https://img.shields.io/github/forks/kinadog/FolderTidy?style=flat-square&logo=github)](https://github.com/kinadog/FolderTidy/network/members)
[![Release downloads](https://img.shields.io/github/downloads/kinadog/FolderTidy/total?style=flat-square&logo=github)](https://github.com/kinadog/FolderTidy/releases)
[![License](https://img.shields.io/github/license/kinadog/FolderTidy?style=flat-square)](LICENSE)
[![Last commit](https://img.shields.io/github/last-commit/kinadog/FolderTidy?style=flat-square&logo=github)](https://github.com/kinadog/FolderTidy/commits/main)

Windows용 폴더 정리 도구입니다. 다운로드 폴더처럼 파일이 한곳에 쌓인 디렉터리를 **종류별로 묶어 보고**, **삭제**와 **백업(이동)** 으로 정리하는 흐름에 맞춰 만들었습니다.

> **Platform:** Windows only (WPF)  
> **Runtime:** [.NET 10 SDK](https://dotnet.microsoft.com/download)

## 주요 기능

### 파일 목록
- 폴더 스캔 (하위 폴더 포함 옵션)
- 파일 종류별 그룹: 설치 파일, 압축, 이미지, 그래픽 디자인, CAD/도면, 문서, 동영상, 오디오, 코드, 기타
- 그룹 정렬: 총 용량, 파일 수, 최신 생성일
- 세부 그룹: 생성일/수정일(월별), 파일 크기
- **폴더 점유율** 막대: 각 그룹·파일이 전체 용량의 몇 %인지 표시
- 컴팩트 목록 + 파일 형식 아이콘 (이미지는 선택 시 썸네일)
- 파일명 **더블클릭**으로 실행
- 미리보기 패널: 이미지, PDF, 텍스트/JSON/XML, 압축 파일 트리

### 삭제 예정
- 목록에서 **삭제** → 삭제 예정 탭으로 이동 (목록에서 흐리게 표시)
- 탭에서 검토 후 **완전 삭제** (휴지통 경유 없음)
- 목록 탭에서도 **복원** 가능

### 백업 예정
- 목록에서 **백업** → 백업 예정 탭으로 이동
- 백업 폴더 경로 지정 (필수)
- **파일 종류별 자동 폴더 생성** 옵션 (예: `이미지`, `압축 파일` 하위 폴더)
- 실행 시 선택한 백업 폴더로 파일 **이동** (원본 위치에서 제거)

### 단축키 (파일 목록 탭)
| 키 | 동작 |
|----|------|
| `Insert` | 선택 항목 백업 예정 |
| `Delete` | 선택 항목 삭제 예정 |
| `Ctrl` / `Shift` | 다중 선택 |

## 스크린샷

### 메인 화면 — 종류별 그룹과 점유율

![FolderTidy 메인 화면 — 종류별 그룹, 점유율 막대, 미리보기 패널](docs/images/main-list.png)

### 파일 목록 — 그룹 펼치기, 다중 선택, 백업·삭제 예정

![FolderTidy 파일 목록 — 그룹 펼침, 선택 항목, 단축키 안내](docs/images/file-list-selection.png)

### 삭제 예정 탭

![FolderTidy 삭제 예정 탭 — 최종 검토 후 완전 삭제](docs/images/delete-pending.png)

### 백업 예정 탭

![FolderTidy 백업 예정 탭 — 백업 경로, 종류별 폴더 옵션](docs/images/backup-pending.png)

## 빌드 및 실행

### 개발 실행 (.NET SDK 필요)

```powershell
git clone https://github.com/kinadog/FolderTidy.git
cd FolderTidy
dotnet run --project FolderTidy/FolderTidy.csproj
```

### 포터블 단일 EXE (Release)

.NET 런타임 설치 없이 Windows 64비트에서 바로 실행할 수 있는 **단일 `FolderTidy.exe`** 를 만들 수 있습니다.

```powershell
dotnet publish FolderTidy/FolderTidy.csproj -p:PublishProfile=Portable-Win-x64
```

빌드 결과:

```
artifacts/portable/FolderTidy.exe
```

- **설치 불필요** — exe 하나만 복사해서 실행 (USB 등에 넣어도 됨)
- **Self-contained** — .NET 10 런타임 포함 (약 65MB)
- PDF 미리보기(Docnet) 등 **네이티브 라이브러리**는 실행 시 임시 폴더에 풀립니다. 파일 하나로 배포하지만, 내부적으로는 첫 실행 때 `%TEMP%` 등에 추출됩니다.

GitHub Release에 올릴 때는 위 exe를 zip으로 압축해 첨부하면 README의 **Release downloads** 배지에 다운로드 수가 집계됩니다.

```powershell
Compress-Archive -Path artifacts\portable\FolderTidy.exe -DestinationPath artifacts\FolderTidy-v1.0.0-win-x64-portable.zip -Force
```

## 프로젝트 구조

```
FolderTidy/
├── FolderTidy.slnx
├── README.md
└── FolderTidy/
    ├── FolderTidy.csproj
    ├── MainWindow.xaml          # UI
    ├── ViewModels/              # MVVM
    ├── Services/                # 스캔, 미리보기, 삭제, 백업
    ├── Models/
    ├── Helpers/
    └── Converters/
```

## 기술 스택

- **.NET 10** + **WPF**
- [Docnet.Core](https://www.nuget.org/packages/Docnet.Core) — PDF 미리보기
- [SharpCompress](https://www.nuget.org/packages/SharpCompress) — 압축 파일 트리

## 주의사항

- **완전 삭제**는 복구할 수 없습니다. 삭제 예정 탭에서 최종 확인 후 실행하세요.
- **백업 실행**은 파일을 **이동**합니다. 원본 경로의 파일은 사라집니다.
- 그래픽 디자인 파일(`.psd`, `.ai` 등) 미리보기는 Windows/Adobe 썸네일 제공 여부에 따라 달라집니다.

## 기여

Issue와 Pull Request를 환영합니다. 큰 변경은 먼저 Issue로 논의해 주세요.

## 라이선스

[MIT License](LICENSE)

## 작성자

[kinadog](https://github.com/kinadog)
