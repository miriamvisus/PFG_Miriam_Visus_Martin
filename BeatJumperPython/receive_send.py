import socket
import librosa
import numpy as np
import struct
import soundfile as sf
from pydub import AudioSegment
import time


# Dirección IP y puerto del servidor
HOST = '127.0.0.1'
PORT = 8000

# Crear un socket TCP/IP
server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

# Enlazar el socket a la dirección y puerto especificados
server_socket.bind((HOST, PORT))

# Poner el socket en modo de escucha
server_socket.listen(1)

print(f"Servidor escuchando en {HOST}:{PORT}")

# Aceptar conexiones entrantes
client_socket, client_address = server_socket.accept()

print(f"Conexión establecida desde {client_address}")

def receive_audio_data():
    try:

        # Recibe el tamaño del archivo de audio
        size_bytes = b""
        while len(size_bytes) < 4:
            packet = client_socket.recv(4 - len(size_bytes))
            if not packet:
                raise Exception("No se recibieron datos")
            size_bytes += packet
        file_size = struct.unpack('I', size_bytes)[0]
        print("file size: ", file_size)

        # Recibe los datos de audio y reconstruye el archivo de audio
        audio_data = b""
        remaining_bytes = file_size
        while remaining_bytes > 0:
            packet = client_socket.recv(min(1024, remaining_bytes))
            if not packet:
                raise Exception("No se recibieron datos")
            audio_data += packet
            remaining_bytes -= len(packet)

        audio_array = np.frombuffer(audio_data, dtype=np.float32)

        samplerate = 44100

        # Guardar los datos de audio en formato WAV
        sf.write("output_audio.wav", audio_array, samplerate)

        # Convertir el archivo WAV a MP3
        audio = AudioSegment.from_wav("output_audio.wav")
        audio.export("output_audio.mp3", format="mp3")

        # Procesar los datos de audio
        tempo, energy = process_audio_data("output_audio.mp3")
        energy_length = energy.shape[1]
        print("Longitud de la energía:", energy_length)

        return tempo, energy, energy_length

    except Exception as e:
        print(f"Error al recibir datos de audio: {e}")


def process_audio_data(audio_file):
    try:
        # Cargar los datos de audio utilizando librosa.load()
        y, sr = librosa.load(audio_file, sr=None)

        # Calcular el tempo
        tempo = librosa.beat.tempo(y=y, sr=sr)

        # Calcular la energía
        energy = librosa.feature.rms(y=y)

        print("Tempo:", tempo)
        print("Energía:", energy)

        return tempo, energy

    except Exception as e:
        print(f"Error al procesar datos de audio: {e}")


def send_data_to_unity(tempo, energy, energy_length):
    try:
        # Convertir la matriz de energía a una lista plana de valores flotantes
        energy_flat = energy.flatten().tolist()

        # Comprobar si energy contiene solo valores de punto flotante
        if all(isinstance(x, float) for x in energy_flat):

            # Empaquetar los datos de tempo, longitud de energía y energía
            tempo_bytes = struct.pack('f', tempo)
            energy_length_bytes = struct.pack('I', energy_length)
            energy_format = f'{len(energy_flat)}f'
            energy_bytes = struct.pack(energy_format, *energy_flat)

            # Impresiones adicionales para verificar el flujo de datos
            print("Datos de tempo enviados:", tempo_bytes)
            print("Datos de longitud de energía enviados:", energy_length_bytes)
            print("Datos de energía enviados:", energy_bytes)

            # Enviar los datos de tempo, longitud de energía y energía
            client_socket.send(tempo_bytes)
            client_socket.send(energy_length_bytes)
            client_socket.send(energy_bytes)

            # Cerrar la conexión del cliente
            client_socket.close()

        else:
            raise ValueError("La lista 'energy' no contiene solo valores de punto flotante")

    except Exception as e:
        print(f"Error al enviar datos a Unity: {e}")

tempo, energy, energy_length = receive_audio_data()
send_data_to_unity(tempo, energy, energy_length)

# Cerrar la conexión del servidor
server_socket.close()