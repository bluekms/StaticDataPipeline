#if !NET5_0_OR_GREATER
// netstandard2.1 타깃에는 record/init 컴파일에 필요한 IsExternalInit 이 없어 폴리필한다.
// 컴파일러 전용 마커 타입이라 런타임 동작에는 영향이 없다.
namespace System.Runtime.CompilerServices;

internal static class IsExternalInit;
#endif
