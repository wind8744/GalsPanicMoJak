# GalsPanicMoJak

갈스패닉(Gals Panic) / Qix 스타일의 **영역 따먹기 게임**입니다. 테두리를 따라 움직이다가 안쪽으로 선을 그어 영역을 닫으면 그 영역이 내 것이 되고, 숨겨진 그림이 드러납니다. 보스가 그리는 중인 선에 닿으면 목숨을 잃습니다. 80% 이상 차지하면 스테이지 클리어. Unity 6, C#.

## 요구 사항

- **Unity 6000.0.84f1** (Unity 6 LTS) — [Unity Hub](https://unity.com/download)로 설치
- Git

## 시작하기

1. 저장소를 클론하고 Unity Hub에서 **Add** → 폴더를 선택해 엽니다.
2. 메뉴 **Tools > Project > Play GalsPanic** 을 누르면 `Assets/Scenes/GalsPanic.unity` 를 열고 바로 실행됩니다.
3. 조작: 방향키 / WASD = 이동 · Space = 스테이지 클리어·게임오버 화면에서 계속

## 규칙

- 플레이어(노란 원)는 차지한 영역의 가장자리(밝은 선) 위로만 움직입니다.
- 어두운 미차지 영역으로 들어가면 궤적을 그리고, 궤적이 다시 가장자리에 닿으면 **보스가 없는 쪽**이 차지됩니다.
- 보스(분홍 원)가 그리는 중인 궤적에 닿으면 목숨 1 감소, 궤적 시작점으로 복귀. 목숨 3개.
- 순찰 적(하늘색 원)은 차지한 영역의 가장자리를 따라 돌며, 닿으면 그리는 중이 아니어도 목숨 1 감소. 스테이지가 오르면 수가 늘고(최대 3) 빨라집니다.
- 시간 제한: 1스테이지 100초, 스테이지마다 5초씩 줄어 최소 60초. 0이 되면 목숨 1 감소 후 타이머가 다시 찹니다. 10초 이하는 빨갛게 표시.
- 목숨을 잃은 뒤 1.5초는 무적(깜빡임).
- 한 번에 15% 이상 차지하면 점수 2배. 목표 비율 달성 시 스테이지 클리어, 다음 스테이지는 보스가 빨라지고 그림이 바뀝니다.
- 그림은 기본적으로 스테이지 번호를 시드로 절차 생성한 풍경입니다. **직접 고른 이미지를 쓰려면** `Assets/StreamingAssets/HiddenImages/` 에 PNG/JPG 를 넣으세요. 이름순으로 1스테이지부터 쓰이며, 4:3 비율에 맞춰 가운데를 잘라 채웁니다. 자세한 내용은 그 폴더의 README.txt 참고.

## 폴더 구조

```
Assets/
  Scenes/GalsPanic.unity        게임 씬 (카메라 + GalsPanicGame). 나머지는 실행 중 코드가 만든다
  Scenes/Main.unity             초기 스캐폴드의 3D 예제 씬 (게임과 무관)
  Scripts/GalsPanic/
    Board.cs                    격자 상태, 영역 채우기(flood fill), 텍스처 렌더링
    HiddenImage.cs              숨겨진 그림 (StreamingAssets 이미지 로드 또는 절차 생성)
    PlayerMover.cs              가장자리 이동, 궤적 그리기
    Boss.cs                     미차지 영역 안에서 튕기는 보스, 궤적 충돌 판정
    Sparx.cs                    가장자리를 순찰하는 적
    GalsPanicGame.cs            스테이지·목숨·점수·시간 제한·HUD (Inspector에서 보드 크기, 목표 비율, 적 속도, 제한 시간 조절)
  Tests/EditMode/               보드 채우기·가장자리·적 이동·충돌 테스트 (Window > General > Test Runner)
  Editor/AutoPlay.cs            Tools > Project > Play GalsPanic 메뉴
  StreamingAssets/HiddenImages/ 숨겨진 그림으로 쓸 이미지 파일을 넣는 곳
```

## 컨벤션

- 클래스와 파일 이름은 PascalCase, 비공개 필드는 `_camelCase`.
- 씬과 에셋은 **Force Text** 직렬화를 사용합니다.
