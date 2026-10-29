using System.Collections.Generic;

/// <summary>
/// 다른 증강 에셋을 끌어와 실행하는 모듈(소환물 · 피날레 …).
/// 설명문이 <c>{증강ID.열}</c> 토큰으로 그 에셋의 수치를 읽을 수 있게 목록을 내준다.
/// </summary>
public interface ILinksAugments
{
    void CollectLinked(List<AugmentData> into);
}
