# Unity Programming Portfolio

Unity와 C#을 활용하여 개발한 VR 프로젝트 중
제가 직접 구현한 주요 소스코드를 정리한 포트폴리오입니다.

본 Repository는 코드 리뷰 및 포트폴리오 제출을 목적으로 구성했으며,
외부 SDK, Unity Asset Store 에셋, 모델, 사운드 등
제3자 리소스는 포함하지 않았습니다.

## Projects

### 1. VR-CineCam
**Intent-Aware Automatic Spectator Camera for VR Gameplay**

VR 플레이어의 행동과 주시 대상을 분석하여
현재 상황에 적합한 3인칭 관전 카메라를 자동으로 생성하고 선택하는 시스템입니다.

주요 구현:
- VR 입력 신호 추출
- Combat / Interaction / Exploration 상황 추정
- Attention Target 추정
- 카메라 Shot 후보 생성
- Shot 품질 평가 및 자동 선택
- 카메라 전환 안정화
- Spectator Camera 전용 Occlusion 처리

**Tech Stack**
- Unity
- C#
- Meta XR / Oculus
- Cinemachine

[View Source Code](./VR-CineCam/)

---

### 2. Reducing Cybersickness for 2D VR Spectators Using a Gaze-Based Stabilized Third-Person View
VR 환경에서 플레이어를 안정적으로 보여주기 위한
3인칭 관전자 카메라 시스템을 구현한 프로젝트입니다.

주요 구현:
- 플레이어 방향에 따른 관전자 시점 선택
- 급격한 시점 변경을 줄이기 위한 Snap / Dwell 처리
- 카메라 Pivot 위치 안정화
- Position / Yaw Low-pass Filtering
- XR Tracking과 Spectator Camera 분리

**Tech Stack**
- Unity
- C#
- Meta XR / Oculus

[View Source Code](./IEEE-VR-Spectator-Camera/)

---

## Repository Scope

This repository contains selected source code written for portfolio review.

Third-party SDKs, Unity Asset Store assets, models, sounds,
and other external resources are not included.

The source files may therefore require the original Unity project
and corresponding dependencies to run.