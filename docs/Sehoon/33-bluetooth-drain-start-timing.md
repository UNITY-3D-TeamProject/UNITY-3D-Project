# 33. 블루투스 배터리 소모 시작 시점 수정

- **우선순위**: 미정
- **상태**: 대기
- **관련 작업**: [07-1. 블루투스 스킬](done/07-1-bluetooth-skill.md), [08. 블루투스 모션 추가](done/08-bluetooth-motion.md), [21. 블루투스 디테일 수정](done/21-bluetooth-detail.md) — 탑승 연출(뛰어오른 뒤 착지 시 탈것 생성) 구조

## 목표
블루투스 배터리 소모를 탑승 스킬을 발동한 순간이 아니라, 실제로 탈것에 탑승한 뒤(탈것 생성 시점)부터 시작한다.

## 원인
탑승하면서 떨어지면(탑승 연출 중 착지하지 못하고 낙하), 아직 탈것에 타지 않은 상태인데도 배터리가 소모된다.

## 현재 상황
- `SpawnVehicle.Mount` (`Assets/_Project/Scripts/Skill/Skills/SpawnVehicle.cs`) 는 발동하자마자 `DrainCost.StartDrain()` 을 호출하고 `IsRiding = true` 로 바꾼다.
- 탈것 오브젝트는 `SpawnVehicle.AttachVehicle()` 에서 생성한다. `SkillMediator.BeginMount` 는 지상에서 탑승하면 뛰어오른 뒤 착지(`NotifyLanded`) 때, 점프 중에 탑승하면 착지 때, 그 밖의 경우에는 바로 `AttachVehicle` 을 호출한다.
- 그래서 탑승 연출 중에 떨어져 착지가 늦어지면, 그동안 탈것이 없는데도 배터리가 소모된다.
- `DrainCost.CanPay` 는 소모 중이 아니면 초당 소모량 이상 남아 있어야 지불할 수 있고, 소모 중이면 항상 지불할 수 있다(하차 허용). 소모 시작이 늦어지면 탑승 연출 중에는 "소모 중이 아님" 상태가 된다.

## 작업 범위 (예상)
- 배터리 소모 시작(`StartDrain`)을 `Mount` 에서 탈것이 생성된 뒤(`AttachVehicle`)로 옮긴다.

## 열린 질문
- [ ] `_vehiclePrefab` 가 비어 있어 탈것을 생성하지 않는 경우: 소모를 시작하지 않는가, 기존처럼 탑승 시점에 시작하는가
- [ ] 탑승 연출 중(소모 시작 전) 하차 입력이 오면: 지금처럼 하차를 허용하는가 (소모 중이 아니라서 `DrainCost.CanPay` 의 잔량 판정을 받게 되는지 작업 전 확인)
