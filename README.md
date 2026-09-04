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

HMD 및 Controller 입력으로부터 사용자 행동 정보를 추출하고,  
상황 추정 결과와 대상 가시성을 기반으로 여러 카메라 후보를 생성·평가한 뒤  
가장 적절한 시점을 선택하도록 구현했습니다.

또한 실제 실행 환경에서 발생하는 과도한 시점 전환과 Occlusion 문제를 줄이기 위해  
다양한 카메라 안정화 로직을 적용했습니다.

**주요 구현**

- HMD / Controller 입력 신호 추출
- Combat / Interaction / Exploration 상태 추정
- 사용자 Attention Target 추정
- 다수의 Camera Shot 후보 생성
- Player / Target Visibility 기반 후보 평가
- 상황 적합도 기반 Camera Selection
- Minimum Hold Time 및 Hysteresis 기반 전환 안정화
- Target Loss Grace Period
- Anchor Smoothing
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
화재 대피와 초기 진압 절차를 훈련할 수 있도록 개발한 Standalone VR 시스템입니다.

VR 사용자 입력과 상호작용, 화재 및 연기 상태 처리, 소화기 사용 과정뿐 아니라  
Photon PUN 기반 네트워크 연결과 PC 관전 기능을 구현했습니다.

또한 다층 건물 환경에서 PC 관전자가 사용자의 현재 위치를 효율적으로 확인할 수 있도록  
층별 Layer 기반 Rendering과 CCTV / Top View Camera 기능을 구성했습니다.

**주요 구현**

- Meta Quest 기반 VR 이동 및 45° Snap Turn
- 화염 접촉 및 연기 노출 상태 처리
- Controller Haptic Feedback
- 소화기 안전핀 제거 / 손잡이 / 노즐 조작
- Particle 기반 소화기 분사 및 화재 진압
- Photon PUN 기반 Room 연결
- Player Join / Leave 처리
- Scene Synchronization
- RPC 기반 Particle Effect 동기화
- VR / Non-VR 실행 환경 구분
- PC Top View / CCTV View
- 사용자 위치에 따른 층별 Layer Rendering

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

HMD의 미세한 방향 변화가 관전자 카메라에 지속적으로 전달되어  
화면이 불필요하게 흔들리는 문제를 줄이기 위해 개발한 카메라 안정화 시스템입니다.

HMD 방향을 일정한 방향 단위로 양자화하고,  
일정 시간 동일한 방향을 유지한 경우에만 카메라 방향을 갱신하도록 구성했습니다.

또한 Position / Yaw Low-Pass Filtering과 Smooth Rotation을 적용하여  
사용자의 빠르고 미세한 움직임이 관전자 화면에 직접적으로 전달되는 것을 완화했습니다.

**주요 구현**

- HMD 방향 기반 사용자 시선 방향 계산
- 24개 Focus Point 기반 방향 Quantization
- 0.8초 응시 유지 조건
- 큰 방향 변화에 대한 Threshold 처리
- Temporal Gating
- 시간 기반 Smooth Rotation
- Position Low-Pass Filtering
- Yaw Low-Pass Filtering
- Camera Pivot 안정화
- Spectator Camera와 XR Tracking 분리

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

# Technical Focus

이 Repository에서는 다음과 같은 실시간 소프트웨어 구현 경험을 확인할 수 있습니다.

## Real-Time XR Interaction

- HMD / Controller Input Processing
- VR Locomotion
- Snap Turn
- Haptic Feedback
- Object Interaction
- Particle Collision
- XR Environment Detection

## Real-Time System Control

- State Estimation
- State-Based Interaction Flow
- Target Tracking
- Camera Candidate Evaluation
- Threshold-Based Control
- Hysteresis
- Temporal Gating
- Grace Period
- Low-Pass Filtering

## Camera & Visualization

- Automatic Camera Selection
- Third-Person Spectator Camera
- Camera Transition Stabilization
- Gaze-Based Camera Control
- Occlusion Handling
- Top View
- CCTV View
- Layer-Based Rendering

## Networking

- Photon PUN
- Room Management
- Player Join / Leave Handling
- Scene Synchronization
- RPC
- Networked Effect Synchronization
- VR / PC Client Separation

## Simulation & Interaction

- Fire / Smoke Interaction
- Player State Management
- Fire Extinguisher Interaction
- Training Scenario Logic
- Multi-Step User Interaction

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

# Repository Scope

This repository contains selected source code written for portfolio review.

Only representative source files that I directly implemented are included.  
Third-party SDKs, Unity Asset Store assets, models, animations, sounds, scenes, prefabs, and other external resources are excluded.

Some source files therefore reference project-specific scripts or third-party dependencies that are not included in this repository.

The source code is provided for implementation review and is not intended to function as a standalone Unity project.

---

# About This Repository

The purpose of this repository is to present my experience in implementing  
real-time XR applications, interactive systems, networking, visualization, and camera-control logic using Unity and C#.

Each project directory contains its own README with a more detailed description of the system and the role of each selected source file.