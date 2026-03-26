# StaticDataPipeline

## 라이브러리
* StaticDataAttribute
* SchemaInfoScanner
* ExtractedDataLoader

## CLI 프로그램
1. ExcelColumnExtractor
2. ExtractedDataValidator

## ExcelColumnExtractor

### 개요

1. C#코드를 기반으로 스키마를 파악
2. 엑셀의 시트에서 C#코드에서 읽어가야 하는 컬럼을 파악
3. 해당 컬럼들만 csv 혹은 여타 포멧으로 출력

### 특징

* C#코드는 기본적으로 클래스의 이름이 엑셀 시트의 이름이다
* 기본적으로 클래스의 변수명은 엑셀 시트의 컬럼 이름이다
* 이름들은 Attribute를 이용해 별도로 지정할 수 있다
* Attribute를 이용해 타입을 지정하지 않았다면 C#에 지정된 타입으로 해당 셀을 읽어온다

### 사용법

```
ExcelColumnExtractor <C#클래스경로> <엑셀파일경로> <출력파일경로>
```


## ExtractedDataValidator

### 개요

ExtractedDataLoader 를 이용해 읽어온 데이터를 C# 코드를 기반으로 유효성 검사를 수행하는 프로그램

### 사용법

```
ExtractedDataValidator <C#클래스경로> <출력파일경로>
```
