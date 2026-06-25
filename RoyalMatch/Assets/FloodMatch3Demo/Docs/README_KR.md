# Flood Match 3 Demo V30 - Editor Always Masks

## 수정사항

V30 기준으로 다시 시작했습니다.

이번 버전은 마스크가 런타임에서만 생성되는 것이 아니라, **에디터 상태에서도 Hierarchy에 존재**하도록 수정했습니다.

## 생성되는 오브젝트

상단 메뉴로 씬을 생성하면 즉시 Hierarchy에 생깁니다.

```txt
Editor Solid Water Masks
├─ Solid Cell Water Mask 0,0
├─ Solid Cell Water Mask 1,0
...
└─ Bottom Board Water Mask
```

## 런타임 동작

`BoardManager`는 Play 중 새 마스크를 무조건 만들지 않고, 먼저 `Editor Solid Water Masks` 안에 있는 기존 마스크를 찾습니다.

```txt
기존 마스크 있음 → 그 마스크 사용
기존 마스크 없음 → 필요한 것만 생성
```

## 에디터에서 직접 조절 가능

`Bottom Board Water Mask`를 선택해서 아래쪽 막는 위치를 직접 옮길 수 있습니다.

`BoardManager`에서 조절 가능한 값:

```txt
Editor Mask Width
Editor Mask Height
Editor Mask Spacing
Editor Bottom Mask Height
Editor Bottom Mask Y Offset
Solid Mask Scale Multiplier
Solid Mask Z Offset
Solid Mask Thickness
```

## 적용 방법

1. 기존 `Assets/FloodMatch3Demo` 폴더를 삭제합니다.
2. 이 버전의 `FloodMatch3Demo` 폴더를 `Assets` 안에 넣습니다.
3. 컴파일이 끝나면 상단 메뉴 실행:

```txt
Flood Match 3 > Create Demo Scene & Assets
```

4. 생성된 씬의 Hierarchy에서 `Editor Solid Water Masks`를 확인합니다.
