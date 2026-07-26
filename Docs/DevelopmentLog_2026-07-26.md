# Pixel Bastion: Orc Frontier

## 개발 일지 — 2026년 7월 26일

### 프로젝트 개요

- 장르: 2D 픽셀 아트 횡스크롤 디펜스
- 개발 환경: Unity 6
- 현재 단계: Pre-Alpha
- 게임 목표: 왼쪽의 아군 타워를 방어하면서 Human 유닛을 소환해 오른쪽에서 몰려오는 Monster를 막는다.

### 게임 이름

**Pixel Bastion: Orc Frontier**

한국어 표기는 **픽셀 바스티온: 오크 프론티어**로 정했다.

작은 픽셀 캐릭터들이 최후의 요새를 지킨다는 게임의 핵심과, 오크를 비롯한 몬스터가 몰려오는 변경 지역이라는 배경을 함께 표현한 이름이다.

---

## 오늘의 개발 목표

레퍼런스 이미지와 같은 횡스크롤 디펜스의 기본 플레이 구조를 완성하고, 보유한 Tiny RPG 캐릭터 에셋을 실제 전투에 사용할 수 있도록 정리하는 것을 목표로 했다.

핵심 플레이 흐름은 다음과 같다.

1. 플레이어가 코인을 사용해 Human 유닛을 소환한다.
2. Human은 왼쪽 타워에서 등장해 오른쪽으로 전진한다.
3. Monster는 오른쪽에서 생성되어 왼쪽 타워를 향해 이동한다.
4. 양 진영은 사거리 안에서 공격 모션과 투사체를 사용해 전투한다.
5. Human이 Monster를 처치하면 몬스터 레벨에 맞는 코인을 얻는다.
6. Monster가 타워를 파괴하면 플레이어가 패배한다.

---

## 구현 내역

### 1. 에셋 분류

Tiny RPG Character Asset Pack의 캐릭터를 두 진영으로 분류했다.

#### Human

- Archer
- Armored Axeman
- Knight
- Knight Templar
- Lancer
- Priest
- Soldier
- Swordsman
- Wizard

#### Monster

- Armored Orc
- Armored Skeleton
- Elite Orc
- Greatsword Skeleton
- Orc
- Orc rider
- Skeleton
- Skeleton Archer
- Slime
- Werebear
- Werewolf

Human과 Monster 에셋을 각 진영 폴더로 정리해 프리팹 생성과 유지보수가 쉬운 구조로 만들었다.

### 2. 횡스크롤 전장 제작

- Tiny RPG 캐릭터와 어울리는 픽셀 아트 맵을 제작했다.
- 맵 이미지를 4개 구간으로 이어 긴 횡스크롤 전장을 구성했다.
- 왼쪽 끝에 아군 타워와 Human 생성 지점을 배치했다.
- 오른쪽 끝에 Monster 생성 지점을 배치했다.
- 맵 구간, 타워, 생성 지점을 Unity Scene에서 직접 이동하고 수정할 수 있도록 구성했다.
- 마우스 드래그로 카메라를 좌우 이동할 수 있게 만들었다.

주요 씬:

`Assets/Scenes/SideDefense.unity`

### 3. 아군 타워

- 플레이어가 방어해야 하는 아군 타워를 제작했다.
- 타워 체력과 체력 바를 연결했다.
- Monster가 타워까지 도달하면 타워를 공격한다.
- 타워 체력이 0이 되면 게임을 정지하고 패배 화면을 표시한다.
- RESTART 버튼으로 전투를 다시 시작할 수 있도록 구성했다.

### 4. Human 소환 시스템

- 화면 아래쪽에 Human 캐릭터 선택 UI를 제작했다.
- 캐릭터 아이콘을 누른 뒤 소환할 수 있도록 구성했다.
- 소환 목록을 가격이 낮은 캐릭터부터 오름차순으로 정렬했다.
- 시작 코인을 200으로 설정했다.
- 마나 대신 골드 코인을 소모하도록 경제 시스템을 변경했다.
- 별도의 골드 코인 이미지도 제작해 UI에 적용했다.
- 소환 UI가 전장 화면을 가리지 않도록 카메라 뷰포트와 화면 비율을 조정했다.
- 소환된 Human의 크기를 맵에 맞게 확대했다.
- Human이 오른쪽을 바라보고 이동하도록 설정했다.

### 5. 체력 시스템

- 아군 타워 체력 바
- Human 캐릭터 머리 위 체력 바
- Monster 캐릭터 머리 위 체력 바

진영을 빠르게 구분할 수 있도록 Human과 타워는 아군 계열 색상, Monster는 붉은 계열 색상으로 표현했다.

### 6. Monster 웨이브

- Monster가 오른쪽에서 생성되어 왼쪽으로 이동하도록 구현했다.
- Monster는 가까운 Human을 우선 공격하고, 방해하는 Human이 없으면 타워로 이동한다.
- 시간이 지날수록 더 강한 Monster 종류가 해금된다.
- 웨이브가 증가할수록 Monster의 체력, 공격력, 이동 속도가 상승한다.
- 시간이 지날수록 Monster 생성 간격도 짧아진다.
- 현재 웨이브와 위협 배율을 화면에 표시한다.

현재 난이도 증가 규칙:

- 웨이브 간격: 30초
- 웨이브당 체력 증가: 18%
- 웨이브당 공격력 증가: 14%
- 이동 속도는 단계적으로 증가
- 생성 간격은 점차 감소

### 7. 공격 모션 및 전투

Human 9종과 Monster 11종, 총 20개 캐릭터의 공격 스프라이트 시트를 프리팹에 연결했다.

- 이동 중에는 Walk 모션을 반복한다.
- 적이 공격 범위에 들어오면 이동을 멈춘다.
- 공격 주기에 맞춰 Attack 모션을 한 번 재생한다.
- 공격이 끝나고 적이 멀리 있으면 다시 이동한다.

#### 원거리 캐릭터

- Human: Archer, Priest, Wizard
- Monster: Skeleton Archer

원거리 공격은 실제 투사체를 생성한다.

- Archer: 화살
- Priest: 성스러운 마법탄
- Wizard: 마법 투사체
- Skeleton Archer: 몬스터 화살

투사체가 목표에게 도달했을 때 피해가 적용되며, 목표가 먼저 사망하면 투사체가 자동으로 제거된다.

#### 근거리 캐릭터

- Human: Soldier, Swordsman, Knight, Knight Templar, Lancer, Armored Axeman
- Monster: Slime, Skeleton, Orc, Armored Skeleton, Armored Orc, Greatsword Skeleton, Werewolf, Elite Orc, Orc rider, Werebear

근거리 캐릭터는 자신의 공격 범위 안에서 공격 모션과 함께 직접 피해를 준다.

### 8. 원거리 사거리 조정

근접 캐릭터 뒤에서 원거리 캐릭터가 안정적으로 공격할 수 있도록 사거리를 확장했다.

| 캐릭터 | 기존 사거리 | 변경 사거리 |
|---|---:|---:|
| Archer | 2.4 | 3.4 |
| Priest | 1.8 | 2.8 |
| Wizard | 2.1 | 3.1 |
| Skeleton Archer | 2.25 | 3.25 |

Skeleton Archer가 타워를 공격할 때 사용하는 사거리도 3.25로 통일했다.

### 9. 레벨별 처치 보상

- 생성 당시의 웨이브를 Monster 레벨로 기록한다.
- Human의 근거리 또는 원거리 공격이 마지막 타격을 했을 때만 코인을 지급한다.
- 환경 피해나 Human 이외의 피해로 죽은 Monster는 코인을 지급하지 않는다.

보상 공식:

`최종 코인 = 기본 코인 × (1 + (몬스터 레벨 - 1) × 0.25)`

계산 결과의 소수점은 올림 처리한다.

예시:

| 기본 보상 | 레벨 | 최종 보상 |
|---:|---:|---:|
| 10 | 1 | 10 |
| 10 | 2 | 13 |
| 10 | 5 | 20 |

---

## 제작된 주요 구조

### Runtime

- `SideDefenseHumanUnit`: Human 이동, 탐색, 공격, 피해 처리
- `SideDefenseMonsterUnit`: Monster 레벨, 이동, 공격, 피해, 처치 보상 정보
- `SideDefenseProjectile`: 원거리 투사체 이동과 피격 처리
- `SideDefenseSpriteAnimator`: Walk 및 Attack 모션 재생
- `SideDefenseMonsterWaveController`: 웨이브, Monster 생성, 난이도, 코인 지급
- `HumanSummonController`: 캐릭터 선택, 소환, 코인 관리
- `SideDefenseTower`: 타워 체력과 파괴 처리
- `SideDefenseGameFlow`: 패배와 재시작 처리

### Prefabs

- Human 프리팹 9개
- Monster 프리팹 11개
- 원거리 투사체 프리팹 4개
- 아군 타워와 체력 바 구성

---

## 검증 결과

별도의 Unity 검증 프로젝트에서 씬과 프리팹을 재생성하고 Play Mode 자동 검증을 진행했다.

- Human 9개 프리팹 생성 확인
- Monster 11개 프리팹 생성 확인
- 투사체 4개 프리팹 생성 확인
- 20/20 캐릭터 공격 시트 연결 확인
- Human과 Monster의 근거리 피해 확인
- Human과 Monster의 원거리 투사체 생성 및 피격 확인
- 거리 3.0에서 양 진영 원거리 공격 성공 확인
- Monster 왼쪽 이동 및 Human 오른쪽 이동 확인
- 타워 파괴 시 패배 처리 확인
- Lv.4 Monster를 Human이 처치했을 때 레벨 보상 지급 확인
- Human 이외의 피해로 처치했을 때 코인이 지급되지 않는 것 확인

---

## 현재 플레이 가능한 상태

현재 게임은 다음 기본 루프를 플레이할 수 있는 상태다.

`Human 선택 → 코인 소모 및 소환 → 전진 → Monster와 자동 전투 → 처치 코인 획득 → 더 강한 웨이브 방어 → 타워 파괴 시 패배`

핵심 시스템이 연결된 첫 번째 플레이 가능한 Pre-Alpha 버전이다.

---

## 다음 개발 후보

1. 게임 시작 화면과 정식 타이틀 로고 적용
2. 승리 조건 및 스테이지 종료 시스템
3. Monster 처치 코인 획득 이펙트와 떠오르는 숫자
4. 공격·피격·사망 사운드
5. 캐릭터별 사망 모션과 피격 모션
6. 공격 대상 선택과 유닛 겹침 개선
7. 캐릭터 능력치 및 소환 가격 밸런싱
8. 웨이브별 보스 Monster
9. 타워 업그레이드와 Human 강화
10. 저장, 옵션, 일시정지 기능

---

## 오늘의 회고

오늘은 단순한 맵과 캐릭터 배치에서 시작해 실제로 반복 플레이가 가능한 횡스크롤 디펜스의 핵심 구조까지 연결했다. 특히 모든 캐릭터의 기존 공격 모션을 활용하면서 근거리와 원거리 공격을 구분했고, 원거리 공격을 실제 투사체 기반으로 구현한 것이 중요한 진전이었다.

다음 작업에서는 플레이어가 공격과 보상의 결과를 더 명확하게 느낄 수 있도록 처치 효과, 코인 획득 연출, 사운드와 스테이지 목표를 우선 추가하는 것이 좋다.
