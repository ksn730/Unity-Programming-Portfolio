# VR Fire Training & Real-Time Spectator System

## Standalone VR Fire Evacuation and Initial Fire Suppression Training

실제 ERICA 학술정보관을 가상 공간으로 재현하여  
화재 대피와 소화기 사용을 훈련할 수 있도록 개발한 Standalone VR 프로젝트입니다.

VR 이동 및 상호작용, 화재·연기 상태 처리, 소화기 조작,  
Photon PUN 기반 PC 실시간 관전 시스템을 구현했습니다.

<p align="center">
  <img src="./Images/overview.png" width="950">
</p>

---

## Project Overview

**Period**  
2023.12 ~ 2024.04

**Type**  
2인 팀 대학 졸업 프로젝트

**Role**  
Unity 기반 시스템 구현 및 프로그래밍 전반

**Tech Stack**  
Unity · C# · Meta Quest 3 · Photon PUN · Unity XR

---

## Main Features

### VR Interaction

- Controller 기반 VR 이동
- VR Avatar 움직임 추적
- 소화기 잡기 및 단계별 조작
- Controller Haptic Feedback

### Fire & Smoke Simulation

- 화염 / 연기 Particle
- 화염 접촉 시 사용자 상태 변화
- 연기 노출량에 따른 시야 흐림
- 연기 단계에 따른 기침음 변화
- Particle 기반 화재 진압

### Performance Optimization

실제 건물 1~4층과 다수의 오브젝트를 포함하면서  
Standalone VR에서 Rendering 성능 저하가 발생했습니다.

Occlusion Culling을 적용하여  
현재 시점에서 불필요한 오브젝트 Rendering을 줄였습니다.

---

## Network Spectator System

<p align="center">
  <img src="./Images/system-architecture.png" width="1000">
</p>

VR Training Client와 PC Spectator Client를 Photon PUN으로 연결하여  
VR 사용자의 상태를 PC 관전자에게 전달했습니다.

### Photon PUN

- Room Connection
- Player Join / Leave
- Transform Sync
- RPC
- Scene Sync

### PC Spectator Client

- VR / Non-VR 실행 환경 구분
- Top View Camera
- CCTV Camera
- Trigger 기반 CCTV Camera 자동 전환
- 플레이어의 현재 층에 따른 Layer Rendering

고정된 화재 진행이나 UI처럼 네트워크 동기화가 필요하지 않은 요소는  
각 Client에서 Local로 처리하도록 구성했습니다.

---

## Key Source Files

프로젝트의 핵심 시스템 구현은 다음 파일에서 확인할 수 있습니다.

### Networking & Spectator

#### [`PhotonLauncher1.cs`](./Source/Network/PhotonLauncher1.cs)
Photon PUN 연결, Room 참가/생성, Player 입퇴장 처리, Scene Sync를 담당합니다.

#### [`NonVRPlayer.cs`](./Source/Network/NonVRPlayer.cs)
XR 실행 여부를 확인하여 VR / Non-VR 환경을 구분하고,  
PC Spectator Camera와 CCTV Trigger 기반 관전 기능을 구성합니다.

#### [`CameraFilter1.cs`](./Source/Network/CameraFilter1.cs)
관전 대상 플레이어의 높이를 기준으로 현재 층을 판단하고,  
Camera Culling Mask와 위치를 변경하여 층별 Top View를 구성합니다.

---

### Fire & Player State

#### [`PlayerState.cs`](./Source/FireSimulation/PlayerState.cs)
연기 및 화염 노출 수치를 관리하고,  
시야 흐림, 기침 효과, Game Over 상태를 처리합니다.

#### [`TouchingFire.cs`](./Source/FireSimulation/TouchingFire.cs)
Particle Collision을 이용해 화염 접촉을 판정하고,  
충돌 방향에 따라 좌·우 Controller Haptic Feedback을 적용합니다.

---

### Fire Extinguisher Interaction

#### [`PinRemove.cs`](./Source/Extinguisher/PinRemove.cs)
소화기 안전핀 제거 과정과 Controller 진동 피드백을 처리합니다.

#### [`PlayExtinguisher.cs`](./Source/Extinguisher/PlayExtinguisher.cs)
Photon RPC를 이용해 소화기 분사 Particle Effect를 모든 Client에 동기화합니다.

---

### VR Locomotion

#### [`VRMove.cs`](./Source/Player/VRMove.cs)
HMD 방향 기준 이동과 45° Snap Turn을 처리합니다.

---

## Additional Source Files

다음 파일들은 핵심 시스템을 보조하는 상호작용 및 상태 처리 코드입니다.

### Fire Simulation

- [`BreathSmoke.cs`](./Source/FireSimulation/BreathSmoke.cs)  
  연기 Particle 충돌과 연기 노출량 증가를 처리합니다.

### Fire Extinguisher

- [`ExtinguisherUse.cs`](./Source/Extinguisher/ExtinguisherUse.cs)  
  소화기 사용 단계의 상태를 관리합니다.

- [`HandleUse.cs`](./Source/Extinguisher/HandleUse.cs)  
  소화기 손잡이 상호작용을 처리합니다.

- [`NozzleHandle.cs`](./Source/Extinguisher/NozzleHandle.cs)  
  왼손 노즐 상호작용과 분사 준비 상태를 처리합니다.

- [`ExtinguishFire.cs`](./Source/Extinguisher/ExtinguishFire.cs)  
  소화기 Particle Trigger에 따라 화염 크기와 Audio 상태를 감소시킵니다.

### Spectator Support

- [`CctvFireLayerChange.cs`](./Source/Network/CctvFireLayerChange.cs)  
  CCTV 관전 시 화재 오브젝트의 Layer를 변경하고 원래 상태로 복원합니다.

---

## Repository Scope

This directory contains selected source code written for portfolio review.

Only representative implementation files are included.  
Some project-specific scripts, third-party SDKs, scenes, prefabs, and external assets are omitted.

The source files are therefore not intended to function as a standalone Unity project.