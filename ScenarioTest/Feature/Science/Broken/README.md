# 깨진 워크북 — 추출 단계 실수 시나리오의 입력

정상 워크북(`Science/Excel`)의 시트 하나를 한 가지 방식으로 망가뜨린 파일들이다.
틀린 자리는 노란 칸에 굵은 진홍 글씨로 표시했고, 그 칸의 메모에 설명과 전후 값을 적었다.
제목 줄이 있는 시트는 제목 오른쪽에, A1 에서 시작하는 시트는 헤더 행의 Record 밖 열에 `[깨진 예] 이름: 설명` 을 적었다.
열이 지워지거나 밀려 바뀐 칸이 많은 경우는 칸을 칠하지 않고 이 설명만 있다.

폴더는 `<워크북>/<시트>/` 로 나뉘어 정상 워크북과 그 안의 시트 구조를 그대로 따르고, 파일 이름이 사례 이름이다.
예를 들어 `Science/Elements/ElementsSheetRenamed.xlsx` 는 `Science/Excel/Science.xlsx` 의 Elements 시트를 망가뜨린 것이다.
테스트는 `ScenarioWorkspace.UseBroken("<워크북>/<시트>/<사례>")` 로 작업 폴더의 정상 워크북을 그 파일로 덮어쓴 뒤 추출기를 돌린다.
정상 워크북이 바뀌면 여기의 파일들도 같이 고쳐야 한다. Excel 로 열어 고치되 노란 표시와 메모는 유지한다.

| 파일 | 설명 | 추출기가 말해야 하는 것 |
|---|---|---|
| `Science/Elements/ElementsSheetRenamed.xlsx` | 시트 이름을 Element(단수)로 바꿨다. Record 가 가리키는 시트 이름과 같아야 한다. | `The given key 'Science.Elements' was not present in the dictionary.` |
