---
mode: agent
---
Define the task to achieve, including specific requirements, constraints, and success criteria.

빌드 요청이 생긴다면 다음과 같은 경로에서 빌드를 하십시오.

📍 TaskLens 빌드 정보
프로젝트 루트 경로:

c:\Users\이재원\source\repos\TaskLens

빌드 명령어:

cd "c:\Users\이재원\source\repos\TaskLens\TaskLens"
& "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe" TaskLens.sln /p:Configuration=Debug /p:Platform="Any CPU" /verbosity:minimal

빌드된 실행 파일 경로:
c:\Users\이재원\source\repos\TaskLens\TaskLens\bin\Debug\TaskLens.exe

실행 명령어 :
.\TaskLens\bin\Debug\TaskLens.exe