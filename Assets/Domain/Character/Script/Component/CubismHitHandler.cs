using System;
using System.Collections;
using Live2D.Cubism.Framework.Expression;
using Live2D.Cubism.Framework.Raycasting;
using UnityEngine;

public class CubismHitHandler : MonoBehaviour
{
    private CubismRaycaster _raycaster = null;
    private CubismExpressionController _expressionController = null;

    public void Initialize()
    {
        _raycaster = GetComponent<CubismRaycaster>();
        _expressionController = GetComponent<CubismExpressionController>();
        ScreenTapManager.Instance.SetRaycaster(_raycaster);
    }

    private void Update()
    {
        if (ScreenTapManager.Instance.TryGetTapUpPosition(out var hits))
        {
            foreach (var hit in hits)
            {
                if(hit.Drawable is null) continue;
                HandleHit(hit.Drawable.name);
            }
        }
    }

    private void HandleHit(string drawableName)
    {
        // 여기서 터치된 파츠별로 반응
        switch (drawableName)
        {
            case "HitAreaHead":
                Debug.Log("머리 터치 → 표정 변경 or 모션 재생");
                ExpressionChange();
                break;
            case "HitAreaBody":
                Debug.Log("몸통 터치 → 다른 반응");
                break;
        }
    }

    // TODO : 추후 표정 관리 클래스로 분리
    private void ExpressionChange()
    {
        _expressionController.CurrentExpressionIndex =
            GetNextExpressionIndex(_expressionController.CurrentExpressionIndex, 0,
                _expressionController.ExpressionsList.CubismExpressionObjects.Length);
    }

    private int GetNextExpressionIndex(int current, int min, int max)
    {
        return ((current - min + 1) % (max - min + 1)) + min;
    }

    public void ExpressionChange_Btn()
    {
        ExpressionChange();
    }
}
