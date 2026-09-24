using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PaulMapper
{
    internal class ClickableLabel : MonoBehaviour, IPointerClickHandler
    {
        public event EventHandler<PointerEventData> OnClick;

        public void OnPointerClick(PointerEventData eventData)
        {
            OnClick?.Invoke(this, eventData);
        }
    }
}
