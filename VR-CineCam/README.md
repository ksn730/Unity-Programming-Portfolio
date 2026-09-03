# VR-CineCam

## Intent-Aware Automatic Spectator Camera for VR Gameplay

VR 플레이어의 행동과 의도를 분석하여 현재 플레이 상황에 적합한  
3인칭 관전 카메라를 자동으로 생성, 평가, 전환하는 Unity 기반 시스템입니다.

---

## System Overview

```text
VR Input
   ↓
Signal Extractor
   ↓
┌─────────────────────────────┐
│ Situation Estimator         │
│ Attention Target Estimator  │
└─────────────────────────────┘
   ↓
Camera Candidate Generator
   ↓
Shot Evaluator
   ↓
Camera Director
   ↓
Spectator Camera
```

---

## Main Implementation

### `SignalExtractor.cs`

HMD와 Controller로부터 카메라 판단에 필요한 VR 입력 신호를 추출합니다.

### `SituationEstimator.cs`

현재 플레이 상황을 다음 세 가지 요소로 추정합니다.

- Combat
- Interaction
- Exploration

### `AttentionTargetEstimator.cs`

HMD 및 Controller의 방향과 입력 정보를 이용하여  
플레이어가 현재 주의를 기울이고 있는 대상을 추정합니다.

급격한 Target 변경을 줄이기 위해 다음과 같은 안정화 로직을 사용합니다.

- Confidence
- Dwell Time
- Minimum Hold
- Hysteresis

### `CameraCandidateGenerator.cs`

플레이어와 Attention Target의 관계 및 현재 상황을 바탕으로  
여러 종류의 카메라 Shot 후보를 생성합니다.

### `ShotEvaluator.cs`

생성된 카메라 후보를 다음 요소를 기준으로 평가합니다.

- Player Visibility
- Target Visibility
- Joint Visibility
- Occlusion
- Proximity
- Situation Fitness
- Camera Transition Cost

### `CameraDirector.cs`

각 후보의 평가 결과를 바탕으로 최종 Shot을 선택하고  
불필요하게 잦은 카메라 전환을 방지합니다.

카메라 전환 안정화를 위해 다음 요소를 고려합니다.

- Minimum Hold Time
- Score Threshold
- Target Loss Grace Time
- Target Reacquisition
- Camera Blend Lock

### `CameraAnchorUpdater.cs`

카메라 Anchor의 이동을 안정화하여  
플레이어나 Attention Target의 움직임에 따른 급격한 카메라 변화를 줄입니다.

### `TargetGroupUpdater.cs`

Cinemachine Target Group의 Target 변경을 관리하고,  
Target 추가 및 제거 과정에서 발생할 수 있는 급격한 FOV 및 구도 변화를 완화합니다.

### `OcclusionTransparencyHandler.cs`

Spectator Camera와 피사체 사이의 오브젝트를 탐지하여  
관전자 화면에서만 해당 오브젝트를 투명하게 처리합니다.

### `VCamPhysicsPassthrough.cs`

카메라 제어 과정에서 필요한 물리 기반 위치 및 상태 정보를  
Virtual Camera 시스템에 전달하기 위한 보조 기능을 담당합니다.

---

## Tech Stack

- Unity
- C#
- Meta XR / Oculus SDK
- Cinemachine

---

## Repository Structure

```text
VR-CineCam/
├── README.md
└── Source/
    ├── SignalExtractor.cs
    ├── SituationEstimator.cs
    ├── AttentionTargetEstimator.cs
    ├── CameraCandidateGenerator.cs
    ├── ShotEvaluator.cs
    ├── CameraDirector.cs
    ├── CameraAnchorUpdater.cs
    ├── TargetGroupUpdater.cs
    ├── OcclusionTransparencyHandler.cs
    └── VCamPhysicsPassthrough.cs
```

---

## Repository Scope

This repository contains selected core source code written for **VR-CineCam** for portfolio review.

Third-party SDKs, Unity Asset Store assets, models, sounds, scenes, and other external resources are not included.

The source files may require the original Unity project and corresponding dependencies to run.

---

## Project Note

This repository focuses on the implementation of the automatic spectator camera system.

The complete Unity project is not included in order to exclude third-party assets and external resources that are not owned by the author.