# Unity Programming Portfolio

Unity와 C#을 활용하여 개발한 VR 프로젝트 중  
제가 직접 구현한 주요 소스코드를 정리한 포트폴리오입니다.

VR 플레이어의 행동을 분석하는 자동 관전 카메라부터  
HMD 기반 카메라 안정화, Photon PUN 네트워크 관전 시스템,  
VR 상호작용 및 화재 훈련 시스템까지 직접 구현한 코드를 선별하여 정리했습니다.

본 Repository는 **게임 프로그래밍 직군 포트폴리오 및 코드 리뷰**를 목적으로 구성했으며,  
외부 SDK, Unity Asset Store 에셋, 모델, 사운드 등  
제3자 소유의 리소스는 포함하지 않았습니다.

---

# Projects

## 1. VR-CineCam

### Intent-Aware Automatic Spectator Camera for VR Gameplay

VR 게임을 2D 화면으로 시청하는 관전자가 플레이 상황과 행동 의도를  
쉽게 이해할 수 있도록, 플레이어의 행동 맥락과 관심 대상을 추정하여  
적절한 3인칭 관전 시점을 자동으로 선택하는 시스템입니다.

**주요 구현**

- HMD / Controller 입력 신호 추출
- Combat / Interaction / Exploration 상황 추정
- 플레이어 Attention Target 추정
- 다수의 3인칭 Camera Shot 후보 생성
- Player / Target Visibility 및 상황 적합도 기반 Shot 평가
- Minimum Hold Time 및 Hysteresis 기반 카메라 전환 안정화
- Target Loss Grace Period 및 Anchor Smoothing
- Spectator Camera 전용 Occlusion 처리

**Tech Stack**

- Unity
- C#
- Meta Quest 3
- Meta XR / Oculus SDK
- Cinemachine

**Project Type**

- 1인 연구 프로젝트
- ISMAR 2026 Poster 게재 확정

[View Project Source](./VR-CineCam/)

---

## 2. VR Fire Training & Real-Time Spectator System

### Standalone VR Fire Evacuation and Initial Fire Suppression Training

실제 건물 환경을 가상 공간으로 재현하여  
화재 대피와 초기 진압 훈련을 수행할 수 있도록 제작한 Standalone VR 프로젝트입니다.

VR 이동 및 조작, 화재·연기 반응, 소화기 상호작용뿐 아니라  
Photon PUN 기반 네트워크 연결과 PC 실시간 관전 기능을 구현했습니다.

**주요 구현**

- Meta Quest 기반 VR 이동 및 45° Snap Turn
- 화염 접촉 및 연기 노출 상태 처리
- Controller Haptic Feedback
- 소화기 안전핀 제거 / 손잡이 / 노즐 조작
- 소화기 Particle 분사 및 화재 진압
- Photon PUN 기반 Room 연결 및 Scene Sync
- RPC 기반 소화기 분사 효과 동기화
- PC Top View / CCTV View
- 플레이어 위치에 따른 층별 Layer 렌더링

**Tech Stack**

- Unity
- C#
- Meta Quest 3
- Meta XR / Oculus SDK
- Photon PUN
- Unity XR

**Project Type**

- 2인 팀 대학 졸업 프로젝트
- Unity 기반 시스템 구현 및 프로그래밍 전반 담당

[View Project Source](./VR-Fire-Training/)

---

## 3. Gaze-Based VR Spectator Camera Stabilization

### HMD Direction-Based Third-Person Spectator Camera Stabilization

HMD의 미세한 방향 변화가 관전자 카메라에 지속적으로 반영되어  
화면이 불필요하게 흔들리는 문제를 줄이기 위해 개발한  
3인칭 VR 관전자 카메라 안정화 시스템입니다.

플레이어의 HMD 방향을 일정한 방향 단위로 양자화하고,  
Temporal Gating과 Smooth Rotation을 적용하여  
짧고 불필요한 시선 변화에 의한 카메라 회전을 억제했습니다.

**주요 구현**

- HMD 방향 기반 시선 방향 계산
- 24개 Focus Point 기반 방향 Quantization
- 0.8초 응시 유지 조건
- 큰 시선 변화에 대한 Threshold 처리
- 시간 기반 Smooth Rotation
- Position / Yaw Low-Pass Filtering
- Spectator Camera와 XR Tracking 분리
- Camera Pivot 위치 안정화

**Tech Stack**

- Unity
- C#
- Meta Quest 3
- Meta XR / Oculus SDK
- Unity XR

**Project Type**

- 1인 연구 프로젝트
- IEEE VR 2026 Poster / Proceedings 게재

[View Project Source](./IEEE-VR-Spectator-Camera/)

---

# Repository Structure

```text
Unity-Programming-Portfolio/
├── README.md
│
├── VR-CineCam/
│   ├── README.md
│   └── Source/
│
├── VR-Fire-Training/
│   ├── README.md
│   └── Source/
│
└── IEEE-VR-Spectator-Camera/
    ├── README.md
    └── Source/
```

---

# Focus Areas

이 Repository에서는 다음과 같은 Unity / C# 구현 경험을 확인할 수 있습니다.

### VR Interaction

- Meta Quest Controller Input
- VR Locomotion / Snap Turn
- Haptic Feedback
- HMD / Controller Tracking
- VR Object Interaction

### Spectator Camera Systems

- Automatic Camera Selection
- Camera Candidate Generation
- Shot Evaluation
- Camera Transition Stabilization
- Gaze-Based Camera Control
- Occlusion Handling
- Top View / CCTV View

### Gameplay & Simulation

- Fire / Smoke Interaction
- Player State Management
- Fire Extinguisher Interaction
- Particle Collision
- Training Scenario Logic

### Networking

- Photon PUN
- Room Management
- Scene Synchronization
- RPC
- VR / PC Spectator Client

---

# Repository Scope

This repository contains selected source code written for portfolio review.

Only representative source files that I directly implemented are included.  
Third-party SDKs, Unity Asset Store assets, models, animations, sounds, scenes, prefabs, and other external resources are excluded.

Some source files therefore reference project-specific scripts or third-party dependencies that are not included in this repository.

The source code is provided for implementation review and is not intended to function as a standalone Unity project.

---

# About This Repository

The purpose of this repository is to present the implementation and problem-solving process behind my Unity and VR projects.

Each project directory contains its own README with a detailed description of the system and the role of each selected source file.