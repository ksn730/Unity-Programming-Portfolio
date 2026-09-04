# Real-Time XR & Unity Software Portfolio

Unity와 C#을 기반으로 개발한 실시간 XR 시스템 및 인터랙티브 애플리케이션 중  
제가 직접 구현한 주요 소스코드를 정리한 포트폴리오입니다.

VR 사용자 입력 처리, 실시간 상태 추정, 카메라 제어 및 안정화,  
네트워크 동기화, 사용자 인터랙션, 훈련 시뮬레이션 등  
실시간 시스템 구현 과정에서 작성한 핵심 코드를 선별하여 정리했습니다.

본 Repository는 **소프트웨어 개발 역량 및 구현 과정 검토를 위한 포트폴리오**로 구성했으며,  
외부 SDK, Unity Asset Store 에셋, 모델, 사운드 등  
제3자 소유의 리소스는 포함하지 않았습니다.

---

# Projects

## 1. VR-CineCam

### Intent-Aware Automatic Spectator Camera for VR Gameplay

VR 사용자의 행동과 관심 대상을 실시간으로 추정하고,  
현재 상황에 적합한 3인칭 관전 시점을 자동으로 선택하는 시스템입니다.

<p align="center">
  <img src="./VR-CineCam/Images/overview.png" width="850">
</p>

**주요 구현**

- HMD / Controller 기반 입력 신호 추출
- Combat / Interaction / Exploration 상황 추정
- Attention Target 추정
- Camera Shot 후보 생성 및 평가
- Hysteresis / Minimum Hold Time 기반 카메라 전환 안정화
- Occlusion 처리

**Tech Stack**

Unity · C# · Meta Quest 3 · Meta XR / Oculus SDK · Cinemachine

**Project Type**

1인 연구 프로젝트 · ISMAR 2026 Poster 게재 확정

[View Project Details & Source](./VR-CineCam/)

---

## 2. VR Fire Training & Real-Time Spectator System

### Standalone VR Fire Evacuation and Initial Fire Suppression Training

실제 건물 환경을 가상 공간으로 재현하여  
화재 대피와 초기 진압 훈련을 수행할 수 있도록 개발한 Standalone VR 시스템입니다.

<p align="center">
  <img src="./VR-Fire-Training/Images/overview.png" width="850">
</p>

**주요 구현**

- VR 이동 및 Controller Interaction
- 화염 / 연기 노출 상태 처리
- 소화기 조작 및 진압 Interaction
- Photon PUN 기반 네트워크 연결
- PC Top View / CCTV 관전
- 층별 Layer Rendering

**Tech Stack**

Unity · C# · Meta Quest 3 · Photon PUN · Unity XR

**Project Type**

2인 팀 대학 졸업 프로젝트 · Unity 기반 시스템 구현 및 프로그래밍 전반 담당

[View Project Details & Source](./VR-Fire-Training/)

---

## 3. Gaze-Based VR Spectator Camera Stabilization

### HMD Direction-Based Third-Person Spectator Camera Stabilization

HMD의 미세한 방향 변화가 관전자 카메라에 지속적으로 반영되어  
화면이 흔들리는 문제를 줄이기 위해 개발한 카메라 안정화 시스템입니다.

<p align="center">
  <img src="./IEEE-VR-Spectator-Camera/Images/overview.png" width="850">
</p>

**주요 구현**

- HMD 방향 기반 시선 방향 계산
- 24개 Focus Point 기반 방향 Quantization
- 0.8초 Temporal Gating
- Threshold 기반 급격한 방향 변화 처리
- Smooth Rotation
- Position / Yaw Filtering

**Tech Stack**

Unity · C# · Meta Quest 3 · Meta XR / Oculus SDK · Unity XR

**Project Type**

1인 연구 프로젝트 · IEEE VR 2026 Poster / Proceedings 게재

[View Project Details & Source](./IEEE-VR-Spectator-Camera/)

---

# Technical Focus

### Real-Time XR Interaction
HMD / Controller Input · VR Locomotion · Haptic Feedback · Object Interaction

### Camera & Visualization
Automatic Camera Selection · Camera Stabilization · Gaze-Based Control · Occlusion Handling · Top View · CCTV View

### Real-Time System Control
State Estimation · Hysteresis · Temporal Gating · Threshold-Based Control · Low-Pass Filtering

### Networking
Photon PUN · Room Management · Scene Synchronization · RPC · VR / PC Client Separation

---

# Repository Structure

```text
Unity-Programming-Portfolio/
├── README.md
│
├── VR-CineCam/
│   ├── README.md
│   ├── Images/
│   └── Source/
│
├── VR-Fire-Training/
│   ├── README.md
│   ├── Images/
│   └── Source/
│
└── IEEE-VR-Spectator-Camera/
    ├── README.md
    ├── Images/
    └── Source/
```

---

# Repository Scope

This repository contains selected source code written for portfolio review.

Only representative source files that I directly implemented are included.  
Third-party SDKs, Unity Asset Store assets, models, animations, sounds, scenes, prefabs, and other external resources are excluded.

Some source files therefore reference project-specific scripts or third-party dependencies that are not included in this repository.

The source code is provided for implementation review and is not intended to function as a standalone Unity project.