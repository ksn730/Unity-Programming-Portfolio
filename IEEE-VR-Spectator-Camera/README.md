# Reducing Cybersickness for 2D VR Spectators Using a Gaze-Based Stabilized Third-Person View

## VR Spectator Camera System

VR 환경에서 플레이어의 움직임을 외부 관전자에게 안정적으로 보여주기 위한  
3인칭 Spectator Camera 시스템을 구현한 프로젝트입니다.

본 Repository에는 프로젝트에서 제가 직접 작성한  
주요 카메라 제어 및 안정화 소스코드를 정리했습니다.

---

## Main Implementation

### `CenterViewpoint.cs`

플레이어의 시선 방향을 기준으로  
주변 관전자 시점을 일정한 각도 단위로 구분하고 적절한 카메라 위치를 선택합니다.

작은 방향 변화가 발생할 때 카메라가 계속 전환되는 현상을 줄이기 위해  
일정 시간 동일한 후보가 유지된 경우에만 시점을 변경하도록 구현했습니다.

또한 카메라가 목표 위치에 충분히 가까워졌을 때  
위치와 회전을 Snap하여 미세한 흔들림을 줄였습니다.

### `StablePivot.cs`

플레이어 위치 및 Yaw 값을 그대로 카메라에 적용할 경우 발생하는  
미세한 HMD 움직임을 줄이기 위해 Low-pass Filtering을 적용했습니다.

프레임 간 시간 차이에 영향을 덜 받도록  
`deltaTime` 기반 지수형 보간 계수를 사용하여 위치와 회전을 안정화합니다.

### `PositionOnlyPivot.cs`

HMD의 로컬 위치 변화 중 카메라 안정화에 불필요한 성분을 제거하고,  
위치 정보만 부드럽게 추종하는 Pivot을 구현했습니다.

### `SpectatorXRDetach.cs`

Spectator Camera가 XR HMD의 위치 및 회전에 의해  
자동으로 제어되지 않도록 XR Camera Tracking과 분리합니다.

또한 Spectator Camera의 Stereo Rendering을 비활성화하여  
일반 디스플레이용 관전자 화면으로 사용할 수 있도록 구성했습니다.

### `FirstPersonSpectatorView.cs`

비교용 First-person spectator view를 구성하기 위한  
기본 카메라 위치 및 회전 추종 로직입니다.

---

## Tech Stack

- Unity
- C#
- Meta XR / Oculus SDK
- Unity XR

---

## Repository Structure

```text
IEEE-VR-Spectator-Camera/
├── README.md
└── Source/
    ├── CenterViewpoint.cs
    ├── StablePivot.cs
    ├── PositionOnlyPivot.cs
    ├── SpectatorXRDetach.cs
    └── FirstPersonSpectatorView.cs
```

---

## Repository Scope

This directory contains selected source code written for portfolio review.

Third-party SDKs, Unity Asset Store assets, models, animations, sounds, scenes, and other external resources are not included.

The source files may require the original Unity project and corresponding dependencies to run.

---

## Project Note

This repository focuses on the implementation of the spectator camera system.

The complete Unity project is not included in order to exclude third-party assets and external resources that are not owned by the author.