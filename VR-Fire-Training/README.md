# VR Fire Training & Real-Time Spectator System

## Standalone VR Fire Evacuation and Initial Fire Suppression Training

실제 건물 환경을 가상 공간으로 재현하여  
화재 대피와 초기 진압 훈련을 수행할 수 있도록 제작한 Standalone VR 프로젝트입니다.

VR 이동 및 조작, 화재·연기 상호작용, 소화기 조작 과정,  
Photon PUN 기반 네트워크 연결과 PC 관전 기능을 구현했습니다.

본 디렉터리에는 포트폴리오 검토를 위해  
제가 직접 작성한 주요 C# 소스코드만 선별하여 포함했습니다.

---

## Main Features

- Meta Quest Controller 기반 VR 이동 및 45° Snap Turn
- 화염 접촉에 따른 플레이어 상태 변화 및 Controller Haptic Feedback
- 연기 노출량에 따른 시야 흐림 및 기침 효과
- 소화기 잡기, 안전핀 제거, 손잡이 및 노즐 조작
- Photon RPC 기반 소화기 분사 효과 동기화
- 소화기 분사에 따른 화염 크기 감소
- Photon PUN 기반 Room 연결 및 Scene Sync
- PC 환경에서 Top View / CCTV View 전환
- 플레이어가 위치한 층에 따른 Layer 기반 관전 화면 구성

---

## System Overview

```text
VR Player
   │
   ├─ VR Movement
   ├─ Fire / Smoke Interaction
   └─ Fire Extinguisher Interaction
              │
              ↓
          Photon PUN
              │
     ┌────────┴────────┐
     │                 │
 Room / Player      Scene Sync
 Management
     │
     └────────┬────────┘
              ↓
       Spectator Functions
       ├─ Top View
       ├─ CCTV View
       └─ Floor-based Rendering
```

---

# Main Implementation

## Player

### `VRMove.cs`

Meta Quest Controller 입력을 이용하여 VR 플레이어의 이동과  
45° Snap Turn을 처리합니다.

주요 구현:

- Primary Thumbstick 기반 이동
- HMD 방향을 기준으로 이동 방향 변환
- CharacterController 기반 이동
- 중력 적용
- Secondary Thumbstick 기반 45° Snap Turn
- HMD 위치에 맞춰 CharacterController의 Center 위치 보정

---

## Fire Simulation

### `PlayerState.cs`

화염 및 연기 노출 정도에 따라 플레이어의 상태를 관리합니다.

연기 누적량에 따라 다음 상태 변화를 처리합니다.

- 화면 앞 Fog Material의 투명도 증가
- 연기 노출 단계에 따른 기침 AudioSource 변경
- 일정 시간 연기에 노출되지 않으면 연기 누적량 감소
- 연기 누적량이 일정 기준 이상일 경우 Game Over 처리

또한 화염 접촉 횟수를 별도로 관리하여  
일정 횟수 이상 화염에 접촉했을 경우 Game Over 상태로 전환합니다.

### `TouchingFire.cs`

Particle Collision을 이용하여 플레이어와 화염의 접촉을 감지합니다.

주요 구현:

- Particle Collision Event 확인
- 플레이어의 화염 접촉 횟수 증가
- 화염과 반대 방향으로 CharacterController 이동
- 충돌 위치와 플레이어 Forward 방향을 비교
- 충돌 방향에 따라 Left / Right Controller에 진동 적용

### `BreathSmoke.cs`

연기 Particle과 플레이어의 충돌을 감지하여  
플레이어의 연기 노출 수치를 증가시킵니다.

`hand_smoke_filter` 상태에 따라  
연기 노출 수치의 증가량을 다르게 적용하도록 구현했습니다.

또한 일정 시간 동안 추가적인 연기 충돌이 없을 경우  
`PlayerState`의 연기 감소 처리가 시작되도록 연결합니다.

---

## Fire Extinguisher Interaction

소화기 사용 과정을 여러 단계로 나누어 구현했습니다.

```text
Extinguisher Interaction
        ↓
Safety Pin Removal
        ↓
Handle Interaction
        ↓
Nozzle Interaction
        ↓
Spray Trigger
        ↓
Particle Effect
        ↓
Fire Suppression
```

### `ExtinguisherUse.cs`

소화기 사용 과정의 진행 상태를 `grabNum`으로 관리합니다.

Left Hand가 소화기 영역에 들어온 상태에서  
Controller Grip 입력이 발생하면 다음 사용 단계로 진행하며,  
소화기의 Transform과 안내 UI를 갱신합니다.

### `PinRemove.cs`

소화기 안전핀 제거 상호작용을 처리합니다.

주요 구현:

- Right Hand의 안전핀 영역 진입 감지
- Right Controller Trigger 입력으로 Pin Grab 상태 설정
- 손이 Trigger 영역에서 빠질 때 안전핀 제거 처리
- 안전핀 제거 완료 시 Right Controller에 진동 적용
- 소화기 사용 상태를 다음 단계로 변경

### `HandleUse.cs`

Right Hand가 손잡이 영역에 들어온 상태에서  
Controller Trigger 입력을 감지하여 소화기 사용 단계를 진행합니다.

상호작용 완료 후 소화기의 Transform 및 안내 UI를 변경합니다.

### `NozzleHandle.cs`

Left Hand와 소화기 노즐의 상호작용을 처리합니다.

노즐을 잡는 입력이 확인되면 소화기 사용 상태를 변경하고,  
이후 Right Controller Trigger 입력을 통해 분사 준비 단계로 전환합니다.

### `PlayExtinguisher.cs`

소화기 사용 상태가 분사 단계에 도달하면  
해당 PhotonView의 소유자에서 RPC를 호출합니다.

`RpcTarget.All`을 이용해 `ParticlePlay()`를 모든 Client에서 실행하여  
소화기 분사 Particle을 네트워크상에서 재생합니다.

### `ExtinguishFire.cs`

소화기 Particle Trigger가 발생하면 화염의 크기를 점진적으로 줄입니다.

화염 크기가 일정 수준 이하가 되면:

- 화염 Scale을 0으로 설정
- 화염 AudioSource의 Volume을 0으로 변경
- AudioSource를 비활성화

하여 소화 완료 상태를 표현합니다.

---

## Network & Spectator System

### `PhotonLauncher1.cs`

Photon PUN을 이용한 VR 사용자 측 네트워크 연결 및  
훈련 Scene 전환을 처리합니다.

주요 구현:

- Photon 서버 연결
- `JoinOrCreateRoom()`을 이용한 Room 참가/생성
- Room 최대 인원 2명 설정
- Player 입장 / 퇴장 Callback 처리
- 감독관 접속 여부 UI 갱신
- `PhotonNetwork.AutomaticallySyncScene` 활성화
- Master Client에서 훈련 Scene 로드

### `CameraFilter1.cs`

Photon Network에서 다른 플레이어 객체를 관전할 때  
플레이어 머리의 Y 위치를 이용하여 현재 층을 판단합니다.

플레이어가 위치한 층이 변경되면:

- `Camera.main`의 Culling Mask 변경
- 해당 층 Layer 활성화
- Player 및 관전용 Layer 유지
- Main Camera의 Y 위치 변경
- 현재 층 UI Text 갱신

을 수행합니다.

이를 통해 다층 건물의 Top View 관전 화면에서  
플레이어가 있는 층을 중심으로 화면을 구성합니다.

### `NonVRPlayer.cs`

현재 실행 환경에서 XR Display가 실행 중인지 확인하여  
VR 환경과 Non-VR 환경을 구분합니다.

Non-VR 환경에서는 VR 전용 Component들을 비활성화 또는 제거하고  
PC 관전을 위한 Camera 동작을 구성합니다.

주요 구현:

- `XRDisplaySubsystem`을 이용한 XR 실행 여부 확인
- PC 실행 시 VR 전용 Component 비활성화
- Main Camera / Second Camera 전환
- Scene에 따른 관전 Camera 설정
- Mouse Input을 이용한 Camera 전환
- `CctvArea` Trigger 진입 시 지정된 CCTV Camera 위치 및 회전 적용

### `CctvFireLayerChange.cs`

CCTV 관전 화면에서 화재 오브젝트의 렌더링 Layer를 변경하기 위한  
보조 기능입니다.

화재 오브젝트와 지정된 자식 오브젝트의 Layer를  
CCTV용 Layer로 변경하고, 필요 시 원래 Layer 값으로 복원합니다.

---

## Tech Stack

- Unity
- C#
- Meta Quest
- Meta XR / Oculus SDK
- Photon PUN
- Unity XR

---

## Repository Structure

```text
VR-Fire-Training/
├── README.md
└── Source/
    ├── Network/
    │   ├── PhotonLauncher1.cs
    │   ├── CameraFilter1.cs
    │   ├── NonVRPlayer.cs
    │   └── CctvFireLayerChange.cs
    │
    ├── FireSimulation/
    │   ├── PlayerState.cs
    │   ├── TouchingFire.cs
    │   └── BreathSmoke.cs
    │
    ├── Extinguisher/
    │   ├── ExtinguisherUse.cs
    │   ├── PinRemove.cs
    │   ├── HandleUse.cs
    │   ├── NozzleHandle.cs
    │   ├── PlayExtinguisher.cs
    │   └── ExtinguishFire.cs
    │
    └── Player/
        └── VRMove.cs
```

---

## Key Source Files

프로젝트의 주요 구현은 다음 파일에서 확인할 수 있습니다.

- `PhotonLauncher1.cs`
  - Photon PUN 연결, Room 관리 및 Scene Sync

- `CameraFilter1.cs`
  - 플레이어 높이 기반 층 판정과 Layer / Camera 제어

- `NonVRPlayer.cs`
  - VR / Non-VR 실행 환경 구분 및 PC 관전 Camera 처리

- `PlayerState.cs`
  - 화염 및 연기 노출에 따른 플레이어 상태 관리

- `TouchingFire.cs`
  - Particle Collision과 방향 기반 Controller Haptic Feedback

- `PinRemove.cs`
  - 안전핀 제거 과정과 Controller 진동 상호작용

- `PlayExtinguisher.cs`
  - Photon RPC를 이용한 소화기 Particle 동기화

- `VRMove.cs`
  - VR 이동 및 45° Snap Turn

---

## Repository Scope

This directory contains selected source code written for portfolio review.

Only representative implementation files are included.  
Some referenced project-specific scripts, third-party SDKs, scenes, prefabs, and external assets are omitted.

The source files are therefore not intended to function as a standalone Unity project.

---

## Project Note

This project was developed as a two-person university graduation project.

This repository focuses on the Unity and C# implementation that I directly contributed to, including VR interaction, fire and smoke interaction, extinguisher interaction, Photon PUN networking, and spectator functionality.

Third-party SDKs, external assets, models, animations, sounds, and other resources not owned by the author are not included.