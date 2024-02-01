namespace company.BettingOnColors.Utility
{
    public static class IntArrayUtility
    {
        public static int[] Zeros(int num){
            return Init(num, 0);
        }

        public static int[] Init(int num, int value){
            var result = new int[num];
            for(int i = 0; i < num; i++){
                result[i] = value;
            }
            return result;
        }

        public static int[] Copy(this int[] arr)
        {
            var result = new int[arr.Length];
            for (int i = 0; i < arr.Length; i++)
            {
                result[i] = arr[i];
            }
            return result;
        }

        public static string ChipsToString(int[] chips){
            string result = "[";
            for(int i = 0; i < chips.Length; i++){
                result += chips[i];
                if(i < chips.Length - 1){
                    result += ", ";
                }
            }
            result += "]";
            return result;
        }

        public static int Sum(this int[] arr){
            int result = 0;
            for(int i = 0; i < arr.Length; i++){
                result += arr[i];
            }
            return result;
        }
    }
}