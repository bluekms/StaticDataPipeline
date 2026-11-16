# 깨진 워크북 — 추출 단계 실수 시나리오의 입력

정상 워크북(`Rpg/Excel`)의 시트 하나를 한 가지 방식으로 망가뜨린 파일들이다.
틀린 자리는 노란 칸에 굵은 진홍 글씨로 표시했고, 그 칸의 메모에 설명과 전후 값을 적었다.
제목 줄이 있는 시트는 제목 오른쪽에, A1 에서 시작하는 시트는 헤더 행의 Record 밖 열에 `[깨진 예] 이름: 설명` 을 적었다.
열이 지워지거나 밀려 바뀐 칸이 많은 경우는 칸을 칠하지 않고 이 설명만 있다.

폴더는 `<워크북>/<시트>/` 로 나뉘어 정상 워크북과 그 안의 시트 구조를 그대로 따르고, 파일 이름이 사례 이름이다.
예를 들어 `Rpg/Items/ItemIdOutsideBand.xlsx` 는 `Rpg/Excel/Rpg.xlsx` 의 Items 시트를 망가뜨린 것이다.
테스트는 `ScenarioWorkspace.UseBroken("<워크북>/<시트>/<사례>")` 로 작업 폴더의 정상 워크북을 그 파일로 덮어쓴 뒤 추출기를 돌린다.
정상 워크북이 바뀌면 여기의 파일들도 같이 고쳐야 한다. Excel 로 열어 고치되 노란 표시와 메모는 유지한다.

| 파일 | 설명 | 추출기가 말해야 하는 것 |
|---|---|---|
| `Rpg/Items/ItemPriceThousandsText.xlsx` | 가격을 1,000 처럼 천 단위 구분자가 든 텍스트로 적었다. 숫자만 받는다. | `'1,000'` |
| `Rpg/Items/ItemPriceDecimalText.xlsx` | 정수 열(Price)에 150.0 을 적었다. 소수점은 받지 않는다. | `'150.0'` |
| `Rpg/Items/ItemIdOutsideBand.xlsx` | 아이템 Id 대역은 10000~19999 인데 99999 를 적었다. | `Value(99999) must be between 10000 and 19999` |
| `Rpg/Monsters/MonsterSkillUndeclared.xlsx` | 스킬 목록에 선언되지 않은 Lightning 이 들어갔다. SkillId enum 의 이름만 받는다. | `'Lightning'` |
| `Rpg/Shop/ShopSaleStartWithoutOffset.xlsx` | 판매 시작 시각에 시간대 오프셋(+09:00)이 없다. 이 열의 형식은 yyyy-MM-dd HH:mm:ss zzz 다. | `'2026-03-01 00:00:00'` |
| `Rpg/Items/TagsFirstColumnDeleted.xlsx` | 배열 열 Tags[0] 을 지워 Tags[1], Tags[2] 만 남았다. 인덱스 헤더는 [0] 부터 이어져야 한다. | `Indexed headers for 'Tags' must run contiguously from [0]` |
| `Rpg/Monsters/SpawnMapHeaderShortened.xlsx` | 중첩 헤더 Spawn.Map 을 Spawn 으로 줄여 적었다. 중첩 record 의 열은 바깥.안쪽 이름 전체가 헤더다. | `Header not found: Spawn.Map` |
| `Rpg/Items/ItemsSheetHidden.xlsx` | Items 시트를 숨겼다. 숨긴 시트는 읽지 않고 오류 하나로 알린다. | `is hidden` / `Items` |
| `Rpg/Items/ItemPricesByFormula.xlsx` | 아이템 9개를 더 넣고 12개 행의 Price 를 수식(=100*2)으로 채웠다. 수식 셀은 받지 않으며, 주소는 10개까지만 나열하고 전체 개수를 적는다. | `has formulas in cells` / `(12 cells)` |
| `Rpg/Gacha/GachaRatesAsPercentCells.xlsx` | 확률을 2 가 아니라 0.02 에 퍼센트 서식(0.000%)을 입혀 적었다. 추출은 서식을 경고하고 저장된 값 0.02 를 쓰므로 합이 1 이 되어 로드에서 GachaTable.Validate 가 거부한다. | 실패하지 않는다(경고만 남기거나 다음 단계에서 걸린다) |
| `Rpg/Currencies/CurrencyCapThousandsFormat.xlsx` | Cap 두 칸에 천 단위 서식(#,##0)을 입혔다. 값은 그대로 읽히고 서식만 경고로 알린다. | 실패하지 않는다(경고만 남기거나 다음 단계에서 걸린다) |
