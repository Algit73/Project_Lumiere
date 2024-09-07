using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CardHandler : MonoBehaviour
{
    [SerializeField] private RectTransform cardRectTr;
    [SerializeField] private TextMeshProUGUI questionText;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private TextMeshProUGUI chat_context;
    [SerializeField] private TextMeshProUGUI question_items;
    [SerializeField] private Transform canvas;
    [SerializeField] private Transform mainCam;
    [SerializeField] private Transform camTr1, camTr2, camTr3;

    //private Vector2 _hiddenCardPos = new Vector2(600f, 1180);
    //private Vector2 _shownCardPos = new Vector2(-140f, 440f);
    private static Transform[] _objectsInCard;

    public string QuestionText { set => questionText.text = value; }
    public string ResultText { set => resultText.text = value; }
    public string Chat_Context { set => chat_context.text = value; }
    public string Question_Items { set => question_items.text = value; }

    public static bool CardIsOpen;

    private void Start()
    {
        _objectsInCard = new Transform[3];

        CommandsCenter.Manager.Card = this;
        chat_context.text = "";
        questionText.text = "";
        question_items.text = "";
        resultText.text = "";
        CardIsOpen  = true;
        cardRectTr.gameObject.SetActive(false);
        StartCoroutine(HideCard());
    }
    private void Update()
    {
        // canvas.LookAt(mainCam);
    }

    public IEnumerator ShowCard()
    {
        cardRectTr.gameObject.SetActive(true);

        //Vector2 startPosMin = cardRectTr.offsetMin;
        //Vector2 startPosMax = cardRectTr.offsetMax;
        //Vector2 targetPosMin = cardRectTr.offsetMin;
        //targetPosMin.y = _shownCardPos.y;
        //Vector2 targetPosMax = cardRectTr.offsetMax;
        //targetPosMax.y = _shownCardPos.x;
        //float timer = 0f;

        //while (timer < 1f)
        //{
        //    timer += Time.deltaTime;

        //    cardRectTr.offsetMin = Vector2.Lerp(startPosMin, targetPosMin, timer);
        //    cardRectTr.offsetMax = Vector2.Lerp(startPosMax, targetPosMax, timer);

        //    yield return null;
        //}

        yield return null;

        CardIsOpen = true;
    }
    public IEnumerator HideCard()
    {
        CardIsOpen = false;

        //Vector2 startPosMin = cardRectTr.offsetMin;
        //Vector2 startPosMax = cardRectTr.offsetMax;
        //Vector2 targetPosMin = cardRectTr.offsetMin;
        //targetPosMin.y = _hiddenCardPos.y;
        //Vector2 targetPosMax = cardRectTr.offsetMax;
        //targetPosMax.y = _hiddenCardPos.x;
        //float timer = 0f;

        //while (timer < 1f)
        //{
        //    timer += Time.deltaTime;

        //    cardRectTr.offsetMin = Vector2.Lerp(startPosMin, targetPosMin, timer);
        //    cardRectTr.offsetMax = Vector2.Lerp(startPosMax, targetPosMax, timer);

        //    yield return null;
        //}

        yield return null;

        cardRectTr.gameObject.SetActive(false);
    }
    public void SetImage(Interactive obj, int imageNum)
    {
        if (_objectsInCard[imageNum] != null) return;

        Transform parent;
        string layer;

        /* we assign layer for rendering that object with specific camera that rendering this layer only and
        the object won't render in other cameras and we had to do this for the object and all its children */

        switch (imageNum)
        {
            case 0:
                camTr1.gameObject.SetActive(true);
                parent = camTr1;
                layer = "Card UI 1";
                break;
            case 1:
                camTr2.gameObject.SetActive(true);
                parent = camTr2;
                layer = "Card UI 2";
                break;
            case 2:
                camTr3.gameObject.SetActive(true);
                parent = camTr3;
                layer = "Card UI 3";
                break;
            default:
                parent = transform;
                layer = "Default";
                break;
        }



        var x_rotation = 0; /// to rotate the object to face the camera
        var y_rotation = 0; /// to rotate the object to face the camera
        var z_rotation = 0; /// to rotate the object to face the camera

        var scale_change_x = 0f;
        var scale_change_y = 0f;
        var scale_change_z = 0f;


        var position_change_x = 0f;
        var position_change_y = 0f;
        var position_change_z = 0f;

        /// Assigning the meta data
        Dictionary<string, string> meta = meta_data_extractor(obj);

        // check if interactive[0] is not null
        // if (interactive.Length == 0) return;

        Interactive clone = Instantiate(obj, parent);
        clone.name = obj.name + $"_Card{imageNum}";
        clone.transform.localPosition = clone.ObjectPosAgainstCamera;
        clone.transform.rotation = Quaternion.identity;
        clone.gameObject.layer = LayerMask.NameToLayer(layer);

        foreach (Transform t in clone.transform)
        {
            if (t.name == "IconCanvas(Clone)") Destroy(t.gameObject);
            else ChangeLayer(t, layer);
        }

        /// meta is a dictionary, we check if it has a key called "rotation"
        if (meta.ContainsKey("x_rotation")) x_rotation = int.Parse(meta["x_rotation"]);
        if (meta.ContainsKey("y_rotation")) y_rotation = int.Parse(meta["y_rotation"]);
        if (meta.ContainsKey("z_rotation")) z_rotation = int.Parse(meta["z_rotation"]);

        if (meta.ContainsKey("scale_change_x")) scale_change_x = float.Parse(meta["scale_change_x"]);
        if (meta.ContainsKey("scale_change_y")) scale_change_y = float.Parse(meta["scale_change_y"]);
        if (meta.ContainsKey("scale_change_z")) scale_change_z = float.Parse(meta["scale_change_z"]);

        if (meta.ContainsKey("position_change_x")) position_change_x = float.Parse(meta["position_change_x"]);
        if (meta.ContainsKey("position_change_y")) position_change_y = float.Parse(meta["position_change_y"]);
        if (meta.ContainsKey("position_change_z")) position_change_z = float.Parse(meta["position_change_z"]);




        // clone.transform.RotateAround(clone.transform.position, Vector3.up, y_rotation);
        // Assuming 'clone' is your GameObject
        Vector3 rotationAngles = new Vector3(x_rotation, y_rotation, z_rotation);
        clone.transform.Rotate(rotationAngles, Space.Self);

        // Assuming 'clone' is your GameObject
        // RotateObject(out clone, x_rotation, y_rotation, z_rotation);
        // Assuming 'clone' is your GameObject
        RotateClone(clone.transform, x_rotation, y_rotation, z_rotation);




        // Scaling
        Vector3 newScale = new Vector3(
            clone.transform.localScale.x + scale_change_x,
            clone.transform.localScale.y + scale_change_y,
            clone.transform.localScale.z + scale_change_z
        );
        clone.transform.localScale = newScale;

        // Position
        Vector3 newPosition = clone.transform.position + new Vector3(position_change_x, position_change_y, position_change_z);
        clone.transform.position = newPosition;

        _objectsInCard[imageNum] = clone.transform;
    }

    private static Dictionary<string, string> meta_data_extractor(Interactive obj)
    {
        var interactive = obj.GetComponents<Interactive>();
        var meta = interactive[0].Meta;
        return meta;
    }

    private void RotateClone(Transform cloneTransform, float xRotation, float yRotation, float zRotation)
    {
        Vector3 rotationAngles = new Vector3(xRotation, yRotation, zRotation);
        cloneTransform.Rotate(rotationAngles, Space.Self);
    }





    public static Transform[] ObjectsInCard { get => _objectsInCard; }
    public IEnumerator ResetImage(int imageNum)
    {
        if (_objectsInCard[imageNum] != null)
        {
            Destroy(_objectsInCard[imageNum].gameObject);

            yield return new WaitForSeconds(1f);
        }

        switch (imageNum)
        {
            case 0:
                camTr1.gameObject.SetActive(false);
                break;
            case 1:
                camTr2.gameObject.SetActive(false);
                break;
            case 2:
                camTr3.gameObject.SetActive(false);
                break;
        }
    }
    public int GetEmptyCardNumber()
    {
        for (int i = 0; i < _objectsInCard.Length; i++)
        {
            if (_objectsInCard[i] == null)
                return i;
        }

        return -1;
    }

    public void reset_card_texts()
    {
        Chat_Context = "";
        QuestionText = "";
        Question_Items = "";
        ResultText = "";
    }

    private void ChangeLayer(Transform obj, string layer)
    {
        obj.gameObject.layer = LayerMask.NameToLayer(layer);

        foreach (Transform t in obj)
        {
            t.gameObject.layer = LayerMask.NameToLayer(layer);

            if (t.childCount != 0) ChangeLayer(t, layer);
        }
    }
}