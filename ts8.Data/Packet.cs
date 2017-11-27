using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ts8.Data {
    public enum OP_Enum { REGISTER, TIME, GUESS, SUMMARY, START }
    public enum OD_Enum { NULL, REQUEST, ACK, NOT_ENOUGH_SPACE, ERROR, GUESSED, NOT_GUESSED, TIME_OUT }

    public class Packet {
        public int ID { get; set; }
        public int Data { get; set; }
        public OD_Enum OD { get; set; }
        public OP_Enum OP { get; set; }

        public Packet(int id, int data, OD_Enum answer, OP_Enum operation) {
            ID = id;
            Data = data;
            OD = answer;
            OP = operation;
        }

        public Packet(int id, OD_Enum answer, OP_Enum operation) {
            ID = id;
            OD = answer;
            OP = operation;
            Data = 0;
        }
        public Packet(OD_Enum answer, OP_Enum operation) {
            ID = 0;
            OD = answer;
            OP = operation;
            Data = 0;
        }

        public string Serialize() {
            string result = "OP?" + opFill() + "<<OD?" + odFill() + "<<ID?" + ID.ToString() + "<<DT?" + Data.ToString() + "<<";
            return result;
        }

        private string opFill() {
            string result = "";// = "OP?";
            if (this.OP == OP_Enum.REGISTER) {
                result += "REGISTER";
            } else if (this.OP == OP_Enum.GUESS) {
                result += "GUESS";
            } else if (this.OP == OP_Enum.START) {
                result += "START";
            } else if (this.OP == OP_Enum.TIME) {
                result += "TIME";
            } else {
                result += "SUMMARY";
            }
            return result;
        }

        public string odFill() {
            string result = "";//"OD?";
            if (this.OD == OD_Enum.ACK) {
                result += "ACK";
            } else if (this.OD == OD_Enum.ERROR) {
                result += "ERROR";
            } else if (this.OD == OD_Enum.GUESSED) {
                result += "GUESSED";
            } else if (this.OD == OD_Enum.NOT_ENOUGH_SPACE) {
                result += "NOT_ENOUGH_SPACE";
            } else if (this.OD == OD_Enum.NOT_GUESSED) {
                result += "NOT_GUESSED";
            } else if (this.OD == OD_Enum.NULL) {
                result += "NULL";
            } else if (this.OD == OD_Enum.TIME_OUT){
                result += "TIME_OUT";
            } else {
                result += "REQUEST";
            }
            return result;
        }

        public static OP_Enum opEnum(string Enum) {
            OP_Enum res;
            if (Enum == "REGISTER") {
                res = OP_Enum.REGISTER;
            } else if (Enum == "GUESS") {
                res = OP_Enum.GUESS;
            } else if (Enum == "START") {
                res = OP_Enum.START;
            } else if (Enum == "SUMMARY") {
                res = OP_Enum.SUMMARY;
            } else {
                res = OP_Enum.TIME;
            }
            return res;
        }

        private static OD_Enum odEnum(string Enum) {
            OD_Enum res;
            if (Enum == "ACK") {
                res = OD_Enum.ACK;
            } else if (Enum == "ERROR") {
                res = OD_Enum.ERROR;
            } else if (Enum == "GUESSED") {
                res = OD_Enum.GUESSED;
            } else if (Enum == "NOT_ENOUGH_SPACE") {
                res = OD_Enum.NOT_ENOUGH_SPACE;
            } else if (Enum == "NOT_GUESSED") {
                res = OD_Enum.NOT_GUESSED;
            } else if (Enum == "NULL") {
                res = OD_Enum.NULL;
            } else {
                res = OD_Enum.REQUEST;
            }
            return res;
        }

        public static Packet Deserialize(string recString) {
            string[] separatingChars = { "<<", "OP?", "OD?", "DT?", "ID?" };
            string[] words = recString.Split(separatingChars, System.StringSplitOptions.RemoveEmptyEntries);
            int data, id;
            Int32.TryParse(words[3], out data);
            Int32.TryParse(words[2], out id);
            //return new Packet(odEnum(recString), opEnum(recString), id, data);
            return new Packet(id, data,odEnum(recString),opEnum(recString));
        }

        public void printPacketInfo() {
            string Od, Op;
            Od = odFill();
            Op = opFill();
            Console.WriteLine("OP = {0}", Op);
            Console.WriteLine("OD = {0}", Od);
            Console.WriteLine("ID = {0}", this.ID);
            Console.WriteLine("DT = {0}", this.Data);
            //return $"OP={opFill}";
        }

        static void Main(string[] args) {
            Packet p = Deserialize("OP?START<<OD?ACK<<ID?0<<DT?0<<");
            p.printPacketInfo();
            string s = p.Serialize();
            Console.WriteLine(s);
        }
    }
}
